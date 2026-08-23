using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Exiled.API.Features;

namespace Capy.API.DiscordBridge;

public sealed class DiscordLinkService : IDisposable
{
    private static readonly JsonSerializerOptions StorageJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly object _sync = new();
    private readonly DiscordBridgeConfig _config;
    private readonly string _storagePath;
    private readonly RandomNumberGenerator _random = RandomNumberGenerator.Create();
    private Dictionary<string, DiscordLinkRecord> _linksByGameUserId = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<ulong, string> _gameUserIdByDiscordId = new();
    private readonly Dictionary<string, PendingDiscordLink> _pendingByCode = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _failedAttemptsByCode = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTime> _lastAttemptByUser = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _lockoutUntilByUser = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (int Count, DateTime WindowStartUtc)> _failuresByUser = new(StringComparer.OrdinalIgnoreCase);

    public DiscordLinkService(DiscordBridgeConfig config)
    {
        _config = config;
        _storagePath = Path.Combine(Paths.Configs, "CapyLib", $"discord_links_{Server.Port}.json");
        Load();
    }

    public LinkCodeResponse CreateCode(LinkCodeRequest request)
    {
        if (request.DiscordUserId == 0)
            throw new ArgumentException("discord_user_id не заполнен.");

        lock (_sync)
        {
            DateTime now = DateTime.UtcNow;
            RemoveExpiredCodesUnsafe(now);

            foreach (string oldCode in _pendingByCode
                         .Where(pair => pair.Value.DiscordUserId == request.DiscordUserId)
                         .Select(pair => pair.Key)
                         .ToList())
            {
                RemovePendingCodeUnsafe(oldCode);
            }

            string code;
            do
            {
                code = GenerateSixDigitCodeUnsafe();
            }
            while (_pendingByCode.ContainsKey(code));

            int lifetimeMinutes = Math.Max(1, Math.Min(_config.LinkCodeLifetimeMinutes, 15));
            DateTime expiresUtc = now.AddMinutes(lifetimeMinutes);
            _pendingByCode[code] = new PendingDiscordLink
            {
                Code = code,
                DiscordUserId = request.DiscordUserId,
                DiscordUserName = NormalizeDiscordName(request.DiscordUserName),
                CreatedAtUtc = now,
                ExpiresAtUtc = expiresUtc
            };

            return new LinkCodeResponse
            {
                Code = code,
                ExpiresInSeconds = lifetimeMinutes * 60,
                ExpiresAt = expiresUtc.ToString("o"),
                Instruction = $"Откройте консоль игры (кнопка `~` или Ё) и напишите: .linkdiscord {code}"
            };
        }
    }

    public bool TryRedeemCode(
        string code,
        string gameUserId,
        out DiscordLinkRecord? linkedAccount,
        out string error)
    {
        linkedAccount = null;
        string normalizedCode = (code ?? string.Empty).Trim();
        string normalizedGameUserId = (gameUserId ?? string.Empty).Trim();

        if (!Regex.IsMatch(normalizedCode, @"^\d{6}$"))
        {
            error = "Код должен состоять ровно из 6 цифр.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(normalizedGameUserId))
        {
            error = "Не удалось определить UserId игрока.";
            return false;
        }

        lock (_sync)
        {
            DateTime now = DateTime.UtcNow;
            RemoveExpiredCodesUnsafe(now);

            if (IsUserLockedUnsafe(normalizedGameUserId, now, out TimeSpan lockRemaining))
            {
                error = $"Слишком много неверных попыток. Повторите через {Math.Ceiling(lockRemaining.TotalMinutes)} мин.";
                return false;
            }

            if (!IsCooldownElapsedUnsafe(normalizedGameUserId, now, out TimeSpan cooldownRemaining))
            {
                error = $"Слишком часто. Повторите через {Math.Ceiling(cooldownRemaining.TotalSeconds)} с.";
                return false;
            }

            _lastAttemptByUser[normalizedGameUserId] = now;

            if (!_pendingByCode.TryGetValue(normalizedCode, out PendingDiscordLink? pending))
            {
                RegisterFailedAttemptUnsafe(normalizedGameUserId, now);
                error = "Код не найден или срок его действия истёк. Запросите новый в Discord через /steamsl.";
                return false;
            }

            if (_failedAttemptsByCode.TryGetValue(normalizedCode, out int codeFailures) &&
                codeFailures >= Math.Max(1, _config.LinkMaxFailedAttemptsPerCode))
            {
                RemovePendingCodeUnsafe(normalizedCode);
                RegisterFailedAttemptUnsafe(normalizedGameUserId, now);
                error = "Код заблокирован из-за неверных попыток. Запросите новый в Discord через /steamsl.";
                return false;
            }

            Dictionary<string, DiscordLinkRecord> oldLinks = _linksByGameUserId
                .ToDictionary(pair => pair.Key, pair => Clone(pair.Value), StringComparer.OrdinalIgnoreCase);
            Dictionary<ulong, string> oldIndex = new(_gameUserIdByDiscordId);

            if (_gameUserIdByDiscordId.TryGetValue(pending.DiscordUserId, out string? previousGameUserId))
                _linksByGameUserId.Remove(previousGameUserId);

            if (_linksByGameUserId.TryGetValue(normalizedGameUserId, out DiscordLinkRecord? previousDiscordLink))
                _gameUserIdByDiscordId.Remove(previousDiscordLink.DiscordUserId);

            var record = new DiscordLinkRecord
            {
                GameUserId = normalizedGameUserId,
                DiscordUserId = pending.DiscordUserId,
                DiscordUserName = pending.DiscordUserName,
                DiscordRoleIds = new List<ulong>(),
                LinkedAtUtc = now,
                LastVerifiedAtUtc = now
            };

            _linksByGameUserId[normalizedGameUserId] = record;
            _gameUserIdByDiscordId[pending.DiscordUserId] = normalizedGameUserId;

            try
            {
                SaveUnsafe();
            }
            catch (Exception ex)
            {
                _linksByGameUserId = oldLinks;
                _gameUserIdByDiscordId = oldIndex;
                Log.Error($"[DiscordBridge] Не удалось сохранить привязку Discord: {ex}");
                error = "Не удалось сохранить привязку на сервере.";
                return false;
            }

            _pendingByCode.Remove(normalizedCode);
            _failedAttemptsByCode.Remove(normalizedCode);
            _failuresByUser.Remove(normalizedGameUserId);
            linkedAccount = Clone(record);
            error = string.Empty;
            Log.Info($"[DiscordBridge] Discord {record.DiscordUserName} ({record.DiscordUserId}) успешно привязан к {record.GameUserId}.");
            return true;
        }
    }

    public bool TryGetByGameUserId(string gameUserId, out DiscordLinkRecord? record)
    {
        lock (_sync)
        {
            if (_linksByGameUserId.TryGetValue(gameUserId ?? string.Empty, out DiscordLinkRecord? stored))
            {
                record = Clone(stored);
                return true;
            }

            record = null;
            return false;
        }
    }

    public bool TryGetByDiscordId(ulong discordUserId, out DiscordLinkRecord? record)
    {
        lock (_sync)
        {
            if (_gameUserIdByDiscordId.TryGetValue(discordUserId, out string? gameUserId) &&
                _linksByGameUserId.TryGetValue(gameUserId, out DiscordLinkRecord? stored))
            {
                record = Clone(stored);
                return true;
            }

            record = null;
            return false;
        }
    }

    public LinkedAccountsResponse GetLinkedAccounts()
    {
        lock (_sync)
        {
            return new LinkedAccountsResponse
            {
                Total = _linksByGameUserId.Count,
                Accounts = _linksByGameUserId.Values
                    .OrderBy(link => link.DiscordUserId)
                    .Select(link => new LinkedAccountItem
                    {
                        GameUserId = link.GameUserId,
                        DiscordUserId = link.DiscordUserId,
                        DiscordUserName = link.DiscordUserName,
                        LinkedAt = link.LinkedAtUtc.ToString("o"),
                        Roles = link.DiscordRoleIds.ToList(),
                        CurrentGroup = link.ManagedExiledGroup
                    })
                    .ToList()
            };
        }
    }

    public LinkRoleSyncResponse SyncRoles(ulong discordUserId, List<ulong> roles)
    {
        lock (_sync)
        {
            if (discordUserId == 0 ||
                !_gameUserIdByDiscordId.TryGetValue(discordUserId, out string? gameUserId) ||
                !_linksByGameUserId.TryGetValue(gameUserId, out DiscordLinkRecord? record))
            {
                return new LinkRoleSyncResponse
                {
                    Success = false,
                    Message = "Аккаунт Discord не привязан к серверу."
                };
            }

            record.DiscordRoleIds = NormalizeRoleIds(roles);
            record.LastVerifiedAtUtc = DateTime.UtcNow;

            SaveUnsafe();

            return new LinkRoleSyncResponse
            {
                Success = true,
                GameUserId = gameUserId,
                AppliedGroup = record.ManagedExiledGroup,
                Message = "Роли успешно синхронизированы."
            };
        }
    }

    public void UpdateManagedGroup(string gameUserId, string? group)
    {
        lock (_sync)
        {
            if (_linksByGameUserId.TryGetValue(gameUserId, out var record))
            {
                record.ManagedExiledGroup = group;
                SaveUnsafe();
            }
        }
    }

    private void RemoveExpiredCodesUnsafe(DateTime now)
    {
        List<string> expired = _pendingByCode
            .Where(pair => pair.Value.ExpiresAtUtc <= now)
            .Select(pair => pair.Key)
            .ToList();

        foreach (string code in expired)
        {
            RemovePendingCodeUnsafe(code);
        }

        List<string> staleLocks = _lockoutUntilByUser
            .Where(pair => pair.Value <= now)
            .Select(pair => pair.Key)
            .ToList();

        foreach (string user in staleLocks)
        {
            _lockoutUntilByUser.Remove(user);
            _lastAttemptByUser.Remove(user);
        }

        List<string> staleWindows = _failuresByUser
            .Where(pair => now - pair.Value.WindowStartUtc > TimeSpan.FromMinutes(30))
            .Select(pair => pair.Key)
            .ToList();

        foreach (string user in staleWindows)
        {
            _failuresByUser.Remove(user);
        }

        List<string> staleAttempts = _lastAttemptByUser
            .Where(pair => now - pair.Value > TimeSpan.FromHours(1))
            .Select(pair => pair.Key)
            .ToList();

        foreach (string user in staleAttempts)
        {
            _lastAttemptByUser.Remove(user);
        }
    }

    private void RemovePendingCodeUnsafe(string code)
    {
        _pendingByCode.Remove(code);
        _failedAttemptsByCode.Remove(code);
    }

    private bool IsUserLockedUnsafe(string user, DateTime now, out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;

        if (_lockoutUntilByUser.TryGetValue(user, out DateTime until))
        {
            if (until > now)
            {
                remaining = until - now;
                return true;
            }

            _lockoutUntilByUser.Remove(user);
        }

        return false;
    }

    private bool IsCooldownElapsedUnsafe(string user, DateTime now, out TimeSpan wait)
    {
        wait = TimeSpan.Zero;

        int cooldown = Math.Max(0, _config.LinkRedeemCooldownSeconds);
        if (cooldown == 0 || !_lastAttemptByUser.TryGetValue(user, out DateTime last))
            return true;

        wait = last.AddSeconds(cooldown) - now;
        return wait <= TimeSpan.Zero;
    }

    private void RegisterFailedAttemptUnsafe(string user, DateTime now)
    {
        if (!_failuresByUser.TryGetValue(user, out var window) || now - window.WindowStartUtc > TimeSpan.FromMinutes(30))
            window = (0, now);

        window.Count++;
        _failuresByUser[user] = window;

        int perPlayerLimit = Math.Max(1, _config.LinkMaxFailedAttemptsPerPlayer);
        if (window.Count >= perPlayerLimit)
        {
            _lockoutUntilByUser[user] = now.AddMinutes(Math.Max(1, _config.LinkLockoutMinutes));
            _failuresByUser.Remove(user);
            Log.Warn($"[DiscordBridge] {user} заблокирован на {_config.LinkLockoutMinutes} мин за перебор кодов привязки.");
        }
    }

    private string GenerateSixDigitCodeUnsafe()
    {
        byte[] bytes = new byte[4];
        _random.GetBytes(bytes);
        uint value = BitConverter.ToUInt32(bytes, 0) % 1000000;
        return value.ToString("D6");
    }

    private static string NormalizeDiscordName(string? name)
    {
        string trimmed = (name ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "DiscordUser" : trimmed;
    }

    private static List<ulong> NormalizeRoleIds(IEnumerable<ulong>? roleIds)
    {
        return (roleIds ?? Enumerable.Empty<ulong>())
            .Where(id => id != 0)
            .Distinct()
            .ToList();
    }

    private static DiscordLinkRecord Clone(DiscordLinkRecord record) => new()
    {
        GameUserId = record.GameUserId,
        DiscordUserId = record.DiscordUserId,
        DiscordUserName = record.DiscordUserName,
        LinkedAtUtc = record.LinkedAtUtc,
        LastVerifiedAtUtc = record.LastVerifiedAtUtc,
        DiscordRoleIds = record.DiscordRoleIds?.ToList() ?? new List<ulong>(),
        ManagedExiledGroup = record.ManagedExiledGroup
    };

    private void SaveUnsafe()
    {
        string? directory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var store = new List<DiscordLinkRecord>(_linksByGameUserId.Values);
        string json = JsonSerializer.Serialize(store, StorageJsonOptions);
        File.WriteAllText(_storagePath, json, Encoding.UTF8);
    }

    private void Load()
    {
        lock (_sync)
        {
            try
            {
                if (!File.Exists(_storagePath))
                    return;

                string json = File.ReadAllText(_storagePath, Encoding.UTF8);
                List<DiscordLinkRecord>? records = JsonSerializer.Deserialize<List<DiscordLinkRecord>>(json, StorageJsonOptions);
                _linksByGameUserId.Clear();
                _gameUserIdByDiscordId.Clear();

                foreach (var record in records ?? new List<DiscordLinkRecord>())
                {
                    if (string.IsNullOrWhiteSpace(record.GameUserId) || record.DiscordUserId == 0)
                        continue;

                    _linksByGameUserId[record.GameUserId] = record;
                    _gameUserIdByDiscordId[record.DiscordUserId] = record.GameUserId;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[DiscordBridge] Ошибка загрузки базы привязок: {ex}");
            }
        }
    }

    public void Dispose()
    {
        _random.Dispose();
    }
}
