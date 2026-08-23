using System.Net;
using System.Net.Http;
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
    private const int MaxBootAttempts = 3;
    private const int MaxConsecutiveRejections = 5;

    private static readonly TimeSpan BootRetryDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(15);

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static volatile bool _isRunning;
    private static volatile bool _pendingDeferredUnlock;
    private static string _licenseKey = string.Empty;
    private static string _serverUrl = string.Empty;
    private static float _intervalSeconds = 300f;

    public static event Action? LicenseConfirmed;

    public static bool IsLicenseValid { get; private set; }
    public static string LicenseOwner { get; private set; } = "CapybaraPR (Developer)";

    private static bool DevMode => CapyPlugin.Instance?.Config.ValidateLicense == false ||
                                   string.Equals(_licenseKey, "DEV_LICENSE", StringComparison.OrdinalIgnoreCase);

    public static async Task<LicenseActivation> ActivateAsync(string serverUrl, string licenseKey, float intervalSeconds = 300f)
    {
        _serverUrl = serverUrl;
        _licenseKey = licenseKey;
        _intervalSeconds = (float)Math.Max(MinInterval.TotalSeconds, intervalSeconds);

        if (DevMode)
        {
            IsLicenseValid = true;
            LicenseOwner = "CapybaraPR (Developer Mode)";
            Log.Info("[CapyLib:DRM] Лицензия: Режим разработчика (DEV MODE).");
            return LicenseActivation.Approved;
        }

        if (string.IsNullOrWhiteSpace(_licenseKey))
        {
            IsLicenseValid = false;
            Log.Error("[CapyLib:DRM] Лицензионный ключ не указан! Проверьте config.yml или license.key");
            return LicenseActivation.Blocked;
        }

        for (int attempt = 1; attempt <= MaxBootAttempts; attempt++)
        {
            LicenseState state = await VerifyOnceAsync().ConfigureAwait(false);

            switch (state)
            {
                case LicenseState.Valid:
                    IsLicenseValid = true;
                    Log.Info($"[CapyLib:DRM] Лицензия успешно подтверждена! Владелец: {LicenseOwner}");
                    StartLoop();
                    return LicenseActivation.Approved;

                case LicenseState.Rejected:
                    IsLicenseValid = false;
                    Log.Error("===============================================================");
                    Log.Error(" [CapyLib:DRM] Сервер лицензий отклонил ключ! Загрузка заблокирована.");
                    Log.Error("===============================================================");
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
        Log.Warn("[CapyLib:DRM] Сервер лицензий недоступен. Загрузка модулей отложена до первой успешной проверки.");
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
            float backoff = _intervalSeconds;

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
                            Log.Info($"[CapyLib:DRM] Лицензия успешно подтверждена! Владелец: {LicenseOwner}");
                            NotifyOnMainThread();
                        }
                        break;

                    case LicenseState.Rejected:
                        consecutiveRejections++;
                        Log.Error($"[CapyLib:DRM] Отказ сервера лицензий ({consecutiveRejections}/{MaxConsecutiveRejections}).");

                        if (!_pendingDeferredUnlock && consecutiveRejections >= MaxConsecutiveRejections)
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
            var payload = new Dictionary<string, string>
            {
                ["license_key"] = _licenseKey,
                ["server_ip"] = Server.IpAddress ?? "127.0.0.1",
                ["server_port"] = Server.Port.ToString()
            };

            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await HttpClient.PostAsync(_serverUrl.TrimEnd('/') + "/api/v1/validate", content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.PaymentRequired or HttpStatusCode.Forbidden)
                {
                    IsLicenseValid = false;
                    Log.Error($"[CapyLib:DRM] Сервер лицензий отклонил ключ. Код: {(int)response.StatusCode}");
                    return LicenseState.Rejected;
                }

                Log.Warn($"[CapyLib:DRM] Сервер лицензий вернул код {(int)response.StatusCode}. Трактуется как сетевой сбой.");
                return LicenseState.NetworkError;
            }

            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            LicenseResponse? result = JsonSerializer.Deserialize<LicenseResponse>(body, JsonOptions);

            if (result is not { IsValid: true })
            {
                IsLicenseValid = false;
                Log.Error("[CapyLib:DRM] Сервер лицензий отклонил ключ.");
                return LicenseState.Rejected;
            }

            IsLicenseValid = true;
            LicenseOwner = string.IsNullOrEmpty(result.Owner) ? "Authorized Customer" : result.Owner;
            return LicenseState.Valid;
        }
        catch (Exception ex)
        {
            Log.Warn($"[CapyLib:DRM] Сетевая ошибка при проверке лицензии: {ex.Message}");
            return LicenseState.NetworkError;
        }
    }

    private static void EnforceProtection()
    {
        Log.Error("===============================================================");
        Log.Error(" [CapyLib:DRM] ЛИЦЕНЗИЯ НЕ ДЕЙСТВИТЕЛЬНА! МОДУЛИ CAPYLIB ОТКЛЮЧЕНЫ.");
        Log.Error("===============================================================");

        try
        {
            ModuleManager.DisableAll();
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyLib:DRM] Ошибка при отключении модулей: {ex.Message}");
        }
    }
}
