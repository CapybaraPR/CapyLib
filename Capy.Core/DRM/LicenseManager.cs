using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Capy.Core.Loader;

namespace Capy.Core.DRM;

public enum LicenseState
{
    Valid,
    Rejected,
    NetworkError,
}

public enum LicenseActivation
{
    Approved,
    Blocked,
    Deferred,
}

public static class LicenseManager
{
    private const string MasterSecret = "CAPY_DRM_MASTER_KEY_2026_x89aF_SECURE_CAPYBARA_EXILED";
    private const int MaxBootAttempts = 1;
    private const int MaxConsecutiveRejections = 3;

    private static readonly TimeSpan BootRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(2.5) };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static volatile bool _isRunning;
    private static volatile bool _pendingDeferredUnlock;
    private static string _licenseKey = string.Empty;
    private static string _serverUrl = string.Empty;
    private static float _intervalSeconds = 300f;

    public static event Action? LicenseConfirmed;
    public static event Action? LicenseRejected;

    public static bool IsLicenseValid { get; private set; }
    public static string LicenseOwner { get; private set; } = "CapybaraPR";

    public static async Task<LicenseActivation> ActivateAsync(string serverUrl, string licenseKey, float intervalSeconds = 300f)
    {
        _serverUrl = serverUrl?.TrimEnd('/') ?? string.Empty;
        _licenseKey = licenseKey?.Trim() ?? string.Empty;
        _intervalSeconds = (float)Math.Max(MinInterval.TotalSeconds, intervalSeconds);

        if (string.IsNullOrWhiteSpace(_serverUrl) || string.IsNullOrWhiteSpace(_licenseKey))
        {
            IsLicenseValid = false;
            Log.Error("[CapyLib:DRM] ОШИБКА: URL сервера лицензий или лицензионный ключ не указаны!");
            EnforceProtection();
            return LicenseActivation.Blocked;
        }

        for (int attempt = 1; attempt <= MaxBootAttempts; attempt++)
        {
            LicenseState state = await VerifyOnceAsync().ConfigureAwait(false);

            switch (state)
            {
                case LicenseState.Valid:
                    IsLicenseValid = true;
                    Log.Info($"[DRM] Лицензия подтверждена (Владелец: {LicenseOwner})");
                    StartLoop();
                    return LicenseActivation.Approved;

                case LicenseState.Rejected:
                    IsLicenseValid = false;
                    Log.Error("===============================================================");
                    Log.Error(" [CapyLib:DRM] Сервер лицензий отклонил ключ или не сошлась подпись! Загрузка заблокирована.");
                    Log.Error("===============================================================");
                    EnforceProtection();
                    return LicenseActivation.Blocked;

                case LicenseState.NetworkError:
                    if (attempt < MaxBootAttempts)
                        await Task.Delay(BootRetryDelay).ConfigureAwait(false);
                    break;
            }
        }

        _pendingDeferredUnlock = true;
        IsLicenseValid = false;
        StartLoop();
        Log.Warn("[CapyLib:DRM] Сервер лицензий временно недоступен. Загрузка функционала CapyLib отложена до первой успешной валидации.");
        return LicenseActivation.Deferred;
    }

    public static void Stop()
    {
        _isRunning = false;
    }

    private static void StartLoop()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        int consecutiveRejections = 0;

        Task.Run(async () =>
        {
            float backoff = _pendingDeferredUnlock ? 10f : _intervalSeconds;

            while (_isRunning)
            {
                await Task.Delay(TimeSpan.FromSeconds(backoff)).ConfigureAwait(false);
                if (!_isRunning)
                    break;

                LicenseState state = await VerifyOnceAsync().ConfigureAwait(false);

                switch (state)
                {
                    case LicenseState.Valid:
                        consecutiveRejections = 0;
                        backoff = _intervalSeconds;

                        if (_pendingDeferredUnlock)
                        {
                            _pendingDeferredUnlock = false;
                            IsLicenseValid = true;
                            Log.Info($"[DRM] Лицензия подтверждена (Владелец: {LicenseOwner})");
                            NotifyOnMainThread();
                        }
                        break;

                    case LicenseState.Rejected:
                        consecutiveRejections++;
                        Log.Error($"[CapyLib:DRM] Отказ сервера лицензий ({consecutiveRejections}/{MaxConsecutiveRejections}).");

                        if (consecutiveRejections >= MaxConsecutiveRejections)
                        {
                            EnforceProtection();
                            _isRunning = false;
                        }
                        break;

                    case LicenseState.NetworkError:
                        backoff = Math.Min(backoff * 2f, (float)MaxBackoff.TotalSeconds);
                        break;
                }
            }
        });
    }

    private static void NotifyOnMainThread()
    {
        MEC.Timing.CallDelayed(0f, () => LicenseConfirmed?.Invoke());
    }

    private static async Task<LicenseState> VerifyOnceAsync()
    {
        try
        {
            string ip = Server.IpAddress ?? "127.0.0.1";
            string port = Server.Port.ToString();

            var payload = new Dictionary<string, string>
            {
                ["license_key"] = _licenseKey,
                ["server_ip"] = ip,
                ["server_port"] = port
            };

            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await HttpClient.PostAsync(_serverUrl + "/api/v1/validate", content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.PaymentRequired or HttpStatusCode.Forbidden)
                {
                    IsLicenseValid = false;
                    Log.Error($"[CapyLib:DRM] Сервер лицензий отклонил запрос. Код: {(int)response.StatusCode}");
                    return LicenseState.Rejected;
                }

                Log.Warn($"[CapyLib:DRM] Сервер лицензий вернул код {(int)response.StatusCode}. Сетевая задержка.");
                return LicenseState.NetworkError;
            }

            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            LicenseResponse? result = JsonSerializer.Deserialize<LicenseResponse>(body, JsonOptions);

            if (result is not { IsValid: true })
            {
                IsLicenseValid = false;
                Log.Error("[CapyLib:DRM] Сервер лицензий вернул невалидный статус.");
                return LicenseState.Rejected;
            }

            // 1. Проверка на Replay-атаки (Метка времени не должна расходиться более чем на 5 минут)
            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(nowUnix - result.Timestamp) > 300)
            {
                IsLicenseValid = false;
                Log.Error("[CapyLib:DRM] 🚨 ЗАФИКСИРОВАНА ПОПЫТКА ПОДДЕЛКИ: Устаревшая или несинхронизированная метка времени ответа!");
                return LicenseState.Rejected;
            }

            // 2. Криптографическая проверка подписи HMAC-SHA256
            string rawToSign = $"{result.Status}:{result.Code}:{result.Owner}:{result.ExpirationDate}:{result.Timestamp}:{_licenseKey}";
            string expectedSignature = ComputeHmacSha256(rawToSign, MasterSecret);

            if (!string.Equals(result.Signature, expectedSignature, StringComparison.OrdinalIgnoreCase))
            {
                IsLicenseValid = false;
                Log.Error("[CapyLib:DRM] 🚨 КРИТИЧЕСКАЯ ОШИБКА БЕЗОПАСНОСТИ: Цифровая подпись контроллера не совпадает! Обнаружен фейковый сервер.");
                return LicenseState.Rejected;
            }

            IsLicenseValid = true;
            LicenseOwner = string.IsNullOrEmpty(result.Owner) ? "Authorized Client" : result.Owner;
            return LicenseState.Valid;
        }
        catch (Exception ex)
        {
            Log.Warn($"[CapyLib:DRM] Сетевая ошибка при проверке лицензии: {ex.Message}");
            return LicenseState.NetworkError;
        }
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(keyBytes);
        byte[] hash = hmac.ComputeHash(dataBytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static void EnforceProtection()
    {
        IsLicenseValid = false;
        Log.Error("===============================================================");
        Log.Error(" [CapyLib:DRM] ЛИЦЕНЗИЯ НЕ ДЕЙСТВИТЕЛЬНА! ПОЛНАЯ БЛОКИРОВКА CAPYLIB.");
        Log.Error("===============================================================");

        MEC.Timing.CallDelayed(0f, () =>
        {
            try
            {
                LicenseRejected?.Invoke();
                ModuleManager.DisableAll();
            }
            catch (Exception ex)
            {
                Log.Error($"[CapyLib:DRM] Ошибка при отключении модулей: {ex.Message}");
            }
        });
    }
}
