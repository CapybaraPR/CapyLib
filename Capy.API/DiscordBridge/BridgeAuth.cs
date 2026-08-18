using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Exiled.API.Features;

namespace Capy.API.DiscordBridge;

public sealed class BridgeAuth : IDisposable
{
    private readonly DiscordBridgeConfig _config;
    private RSA? _rsaPublicKey;
    private readonly object _lock = new();

    public BridgeAuth(DiscordBridgeConfig config)
    {
        _config = config;
        InitializeKeys();
    }

    public bool IsSshKeyLoaded => _rsaPublicKey != null;

    public void InitializeKeys()
    {
        lock (_lock)
        {
            _rsaPublicKey?.Dispose();
            _rsaPublicKey = null;

            string keyText = _config.SshPublicKey;

            // 1. Try to read from key path if string is empty or looks like a file path
            if (string.IsNullOrWhiteSpace(keyText) || keyText.EndsWith(".pub", StringComparison.OrdinalIgnoreCase))
            {
                string configDir = Path.Combine(Paths.Configs, "CapyLib");
                string keyFile = string.IsNullOrWhiteSpace(_config.SshPublicKeyPath) ? "aspect_bridge.pub" : _config.SshPublicKeyPath;
                string fullPath = Path.IsPathRooted(keyFile) ? keyFile : Path.Combine(configDir, keyFile);

                if (File.Exists(fullPath))
                {
                    try
                    {
                        keyText = File.ReadAllText(fullPath, Encoding.UTF8).Trim();
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"[DiscordBridge.BridgeAuth] Не удалось прочитать файл публичного SSH ключа '{fullPath}': {ex.Message}");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(keyText))
            {
                try
                {
                    _rsaPublicKey = ParsePublicKey(keyText);
                    if (_rsaPublicKey != null)
                    {
                        Log.Info("[DiscordBridge.BridgeAuth] Открытый SSH/RSA ключ успешно загружен и активирован.");
                    }
                    else
                    {
                        Log.Warn("[DiscordBridge.BridgeAuth] Не удалось распознать формат открытого SSH/RSA ключа.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[DiscordBridge.BridgeAuth] Ошибка парсинга открытого ключа: {ex.Message}");
                }
            }
            else if (_config.RequireSshSignature)
            {
                Log.Warn("[DiscordBridge.BridgeAuth] require_ssh_signature=true, но публичный SSH ключ не найден.");
            }
        }
    }

    /// <summary>
    /// Проверяет аутентификацию входящего HTTP запроса.
    /// </summary>
    public bool ValidateRequest(
        string method,
        string pathAndQuery,
        byte[] bodyBytes,
        string? headerSignature,
        string? headerTimestamp,
        string? headerApiKey,
        out string authError)
    {
        authError = string.Empty;

        // Если включен режим обязательной SSH-подписи
        if (_config.RequireSshSignature)
        {
            if (_rsaPublicKey == null)
            {
                authError = "SSH Public Key is not loaded on server.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(headerSignature) || string.IsNullOrWhiteSpace(headerTimestamp))
            {
                authError = "Missing required cryptographic headers: X-Aspect-Signature and X-Aspect-Timestamp.";
                return false;
            }

            if (!long.TryParse(headerTimestamp, out long timestamp))
            {
                authError = "Invalid X-Aspect-Timestamp header format.";
                return false;
            }

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long drift = Math.Abs(now - timestamp);
            if (drift > Math.Max(10, _config.MaxClockDriftSeconds))
            {
                authError = $"Timestamp expired or system clocks out of sync (drift: {drift}s, max: {_config.MaxClockDriftSeconds}s).";
                return false;
            }

            string bodyHashHex = ComputeSha256Hex(bodyBytes);
            string payloadToVerify = $"{timestamp}\n{method.ToUpperInvariant()}\n{pathAndQuery}\n{bodyHashHex}";
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadToVerify);

            try
            {
                byte[] signatureBytes = Convert.FromBase64String(headerSignature);
                bool valid = VerifySignature(_rsaPublicKey, payloadBytes, signatureBytes);
                if (!valid)
                {
                    authError = "Invalid cryptographic SSH/RSA request signature.";
                    return false;
                }

                return true;
            }
            catch (FormatException)
            {
                authError = "X-Aspect-Signature header is not a valid Base64 string.";
                return false;
            }
            catch (Exception ex)
            {
                authError = $"Signature verification error: {ex.Message}";
                return false;
            }
        }

        // Резервный режим по статическому API-ключу
        if (!string.IsNullOrWhiteSpace(_config.ApiKey) &&
            !_config.ApiKey.Equals("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(headerApiKey, _config.ApiKey, StringComparison.Ordinal))
            {
                return true;
            }

            authError = "Invalid API Key header.";
            return false;
        }

        authError = "No valid authentication method configured.";
        return false;
    }

    public static string ComputeSha256Hex(byte[] data)
    {
        if (data == null || data.Length == 0)
            return "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(data);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static bool VerifySignature(RSA rsa, byte[] data, byte[] signature)
    {
        if (rsa is RSACryptoServiceProvider csp)
        {
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(data);
            return csp.VerifyHash(hash, CryptoConfig.MapNameToOID("SHA256")!, signature);
        }

        using var sha = SHA256.Create();
        byte[] h = sha.ComputeHash(data);
        return rsa.VerifyHash(h, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public static RSA? ParsePublicKey(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        string trimmed = text.Trim();

        // 1. OpenSSH ssh-rsa format
        if (trimmed.StartsWith("ssh-rsa", StringComparison.OrdinalIgnoreCase))
        {
            return ParseOpenSshPublicKey(trimmed);
        }

        // 2. PEM format
        if (trimmed.Contains("-----BEGIN"))
        {
            return ParsePemPublicKey(trimmed);
        }

        // 3. XML format
        if (trimmed.StartsWith("<RSAKeyValue>", StringComparison.OrdinalIgnoreCase))
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(trimmed);
            return rsa;
        }

        // 4. Raw base64 OpenSSH format without prefix
        try
        {
            return ParseOpenSshPublicKey("ssh-rsa " + trimmed);
        }
        catch
        {
            return null;
        }
    }

    private static RSA? ParseOpenSshPublicKey(string openSshKey)
    {
        string[] parts = openSshKey.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string base64 = parts.Length > 1 ? parts[1] : parts[0];
        byte[] raw = Convert.FromBase64String(base64);

        using var ms = new MemoryStream(raw);
        using var reader = new BinaryReader(ms);

        int typeLen = ReadIntBigEndian(reader);
        if (typeLen <= 0 || typeLen > ms.Length) return null;
        byte[] typeBytes = reader.ReadBytes(typeLen);
        string keyType = Encoding.ASCII.GetString(typeBytes);
        if (keyType != "ssh-rsa") return null;

        int expLen = ReadIntBigEndian(reader);
        if (expLen <= 0 || expLen > ms.Length) return null;
        byte[] expBytes = reader.ReadBytes(expLen);

        int modLen = ReadIntBigEndian(reader);
        if (modLen <= 0 || modLen > ms.Length) return null;
        byte[] modBytes = reader.ReadBytes(modLen);

        // Strip leading zero byte if present
        if (modBytes.Length > 0 && modBytes[0] == 0)
        {
            byte[] stripped = new byte[modBytes.Length - 1];
            Buffer.BlockCopy(modBytes, 1, stripped, 0, stripped.Length);
            modBytes = stripped;
        }

        var rsaParams = new RSAParameters
        {
            Exponent = expBytes,
            Modulus = modBytes
        };

        var rsa = new RSACryptoServiceProvider();
        rsa.ImportParameters(rsaParams);
        return rsa;
    }

    private static RSA? ParsePemPublicKey(string pem)
    {
        string base64 = pem
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("-----BEGIN RSA PUBLIC KEY-----", "")
            .Replace("-----END RSA PUBLIC KEY-----", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Trim();

        byte[] der = Convert.FromBase64String(base64);
        var rsa = new RSACryptoServiceProvider();

        if (TryDecodeSubjectPublicKeyInfo(der, out RSAParameters rsaParams))
        {
            rsa.ImportParameters(rsaParams);
            return rsa;
        }

        return null;
    }

    private static bool TryDecodeSubjectPublicKeyInfo(byte[] spki, out RSAParameters parameters)
    {
        parameters = default;
        try
        {
            byte[] rsaOid = { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01 };
            int oidIndex = FindBytes(spki, rsaOid);
            if (oidIndex < 0)
            {
                return TryDecodePkcs1PublicKey(spki, out parameters);
            }

            int bitStringIndex = -1;
            for (int i = oidIndex + rsaOid.Length; i < spki.Length - 2; i++)
            {
                if (spki[i] == 0x03)
                {
                    bitStringIndex = i;
                    break;
                }
            }

            if (bitStringIndex < 0) return false;

            int offset = bitStringIndex + 1;
            ReadDerLength(spki, ref offset);
            offset++;

            byte[] pkcs1 = new byte[spki.Length - offset];
            Buffer.BlockCopy(spki, offset, pkcs1, 0, pkcs1.Length);
            return TryDecodePkcs1PublicKey(pkcs1, out parameters);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryDecodePkcs1PublicKey(byte[] pkcs1, out RSAParameters parameters)
    {
        parameters = default;
        try
        {
            int offset = 0;
            if (pkcs1[offset++] != 0x30) return false;
            ReadDerLength(pkcs1, ref offset);

            if (pkcs1[offset++] != 0x02) return false;
            int modLen = ReadDerLength(pkcs1, ref offset);
            byte[] modBytes = new byte[modLen];
            Buffer.BlockCopy(pkcs1, offset, modBytes, 0, modLen);
            offset += modLen;

            if (modBytes.Length > 0 && modBytes[0] == 0)
            {
                byte[] stripped = new byte[modBytes.Length - 1];
                Buffer.BlockCopy(modBytes, 1, stripped, 0, stripped.Length);
                modBytes = stripped;
            }

            if (pkcs1[offset++] != 0x02) return false;
            int expLen = ReadDerLength(pkcs1, ref offset);
            byte[] expBytes = new byte[expLen];
            Buffer.BlockCopy(pkcs1, offset, expBytes, 0, expLen);

            parameters = new RSAParameters
            {
                Modulus = modBytes,
                Exponent = expBytes
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int ReadDerLength(byte[] data, ref int offset)
    {
        byte b = data[offset++];
        if ((b & 0x80) == 0) return b;

        int count = b & 0x7f;
        int val = 0;
        for (int i = 0; i < count; i++)
        {
            val = (val << 8) | data[offset++];
        }
        return val;
    }

    private static int ReadIntBigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (bytes.Length < 4) return 0;
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    private static int FindBytes(byte[] src, byte[] pattern)
    {
        for (int i = 0; i <= src.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (src[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }

    public static byte[] EncodeOpenSshPublicKey(RSAParameters rsaParams)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        byte[] typeBytes = Encoding.ASCII.GetBytes("ssh-rsa");
        WriteIntBigEndian(writer, typeBytes.Length);
        writer.Write(typeBytes);

        WriteIntBigEndian(writer, rsaParams.Exponent.Length);
        writer.Write(rsaParams.Exponent);

        byte[] mod = rsaParams.Modulus;
        if ((mod[0] & 0x80) != 0)
        {
            WriteIntBigEndian(writer, mod.Length + 1);
            writer.Write((byte)0x00);
            writer.Write(mod);
        }
        else
        {
            WriteIntBigEndian(writer, mod.Length);
            writer.Write(mod);
        }

        return ms.ToArray();
    }

    private static void WriteIntBigEndian(BinaryWriter writer, int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        writer.Write(bytes);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _rsaPublicKey?.Dispose();
            _rsaPublicKey = null;
        }
    }
}
