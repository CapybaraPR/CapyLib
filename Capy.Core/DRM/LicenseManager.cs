using System.Net.Http;
using System.Text;
using System.Text.Json;
using Capy.Core.Loader;

namespace Capy.Core.DRM;

/// <summary>
/// Менеджер валидации лицензии библиотеки и антипиратской защиты.
/// </summary>
public static class LicenseManager
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static bool _isRunning;
    private static string _licenseKey = string.Empty;
    private static string _serverUrl = string.Empty;

    public static bool IsLicenseValid { get; private set; }
    public static string LicenseOwner { get; private set; } = "Unknown";

    public static async Task<bool> VerifyAsync(string serverUrl, string licenseKey)
    {
        _serverUrl = serverUrl;
        _licenseKey = licenseKey;

        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            Log.Error("[CapyLib:DRM] Лицензионный ключ не указан! Проверьте config.yml или license.key");
            IsLicenseValid = false;
            return false;
        }

        try
        {
            var payload = new Dictionary<string, string>
            {
                { "license_key", licenseKey },
                { "server_ip", Server.IpAddress },
                { "server_port", Server.Port.ToString() }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpoint = serverUrl.TrimEnd('/') + "/api/v1/validate";
            var response = await HttpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<LicenseResponse>(responseString, JsonOptions);

                if (result != null && result.IsValid)
                {
                    IsLicenseValid = true;
                    LicenseOwner = string.IsNullOrEmpty(result.Owner) ? "Authorized Customer" : result.Owner;
                    Log.Info($"[CapyLib:DRM] Лицензия успешно подтверждена! Владелец: {LicenseOwner}");
                    return true;
                }
            }

            Log.Error($"[CapyLib:DRM] Сервер лицензий отклонил ключ. Код: {response.StatusCode}");
            IsLicenseValid = false;
            return false;
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyLib:DRM] Ошибка при проверке лицензии: {ex.Message}");
            IsLicenseValid = false;
            return false;
        }
    }

    public static void StartLicenseLoop(string serverUrl, string licenseKey, float intervalSeconds = 300f)
    {
        _isRunning = true;
        _serverUrl = serverUrl;
        _licenseKey = licenseKey;

        Task.Run(async () =>
        {
            while (_isRunning)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(30f, intervalSeconds)));
                if (!_isRunning) break;

                bool valid = await VerifyAsync(_serverUrl, _licenseKey);
                if (!valid)
                {
                    Log.Error("[CapyLib:DRM] Повторная проверка лицензии провалена! Активация защиты...");
                    EnforceProtection();
                    break;
                }
            }
        });
    }

    public static void Stop()
    {
        _isRunning = false;
    }

    public static void EnforceProtection()
    {
        try
        {
            Log.Error("===============================================================");
            Log.Error(" [CapyLib:DRM] КРИТИЧЕСКАЯ ОШИБКА: ЛИЦЕНЗИЯ НЕ ДЕЙСТВИТЕЛЬНА! ");
            Log.Error("  Использование библиотеки заблокировано разработчиком.        ");
            Log.Error("===============================================================");

            // Отключаем все модули CapyLib
            ModuleManager.DisableAll();

            // Кикаем всех игроков с сообщением
            foreach (var player in Player.List)
            {
                player.Kick("Сервер использует нелицензионную копию CapyLib. Обратитесь к администрации.");
            }

            // Форсируем детонацию и старт раунда для сброса состояния
            Round.Start();
            Warhead.Detonate();
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyLib:DRM] Ошибка при выполнении защиты: {ex}");
        }
    }
}
