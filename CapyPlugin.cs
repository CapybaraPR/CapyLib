using System;
using System.IO;
using Exiled.API.Features;
using Capy.Core.Database;
using Capy.Core.DRM;
using Capy.Core.Loader;
using Capy.Core.Services;
using Capy.Core.Subsystems;
using Capy.Engine;

namespace Capy;

/// <summary>
/// Главный плагин и точка входа библиотеки CapyLib в среду EXILED с защитой DRM и рефлексивной архитектурой подсистем.
/// </summary>
public sealed class CapyPlugin : Plugin<CapyConfig>
{
    public override string Name => "CapyLib";
    public override string Author => "CapybaraPR";
    public override string Prefix => "capylib";
    public override Version Version => new(1, 4, 5);

    public static CapyPlugin Instance { get; private set; } = null!;

    public ModuleLoader Loader => SubsystemRegistry.GetSubsystem<ModuleLoaderSubsystem>()?.Loader ?? null!;
    public IDatabaseProvider Database => SubsystemRegistry.GetSubsystem<DatabaseSubsystem>()?.Database ?? null!;
    private bool _isInitialized;

    public override void OnEnabled()
    {
        Instance = this;

        SafeExecute("Directories", EnsureDirectories);
        SafeExecute("PlayerStateCleaner", PlayerStateCleaner.EnsureSubscribed);

        // Подписываемся на события проверки лицензии
        LicenseManager.LicenseConfirmed += OnLicenseConfirmed;
        LicenseManager.LicenseRejected += OnLicenseRejected;

        // Запуск безусловной DRM-валидации
        SafeExecute("LicenseCheck", RunLicenseActivation);

        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        LicenseManager.LicenseConfirmed -= OnLicenseConfirmed;
        LicenseManager.LicenseRejected -= OnLicenseRejected;
        LicenseManager.Stop();

        ShutdownAllSubsystems();

        Instance = null!;
        base.OnDisabled();
    }

    private void RunLicenseActivation()
    {
        string licenseKey = GetOrCreateLicenseKey();

        LicenseActivation activation = LicenseManager
            .ActivateAsync(Config.LicenseServerUrl, licenseKey, Config.LicenseCheckIntervalSeconds)
            .GetAwaiter().GetResult();

        switch (activation)
        {
            case LicenseActivation.Approved:
                OnLicenseConfirmed();
                break;

            case LicenseActivation.Blocked:
                Log.Error("[CapyLib] ⛔ КРИТИЧЕСКАЯ ОШИБКА: Сервер не имеет активной лицензии. CapyLib заблокирован.");
                ShutdownAllSubsystems();
                break;

            case LicenseActivation.Deferred:
                Log.Warn("[CapyLib] ⏳ Лицензия в режиме ожидания ответа контроллера. Подсистемы будут активированы после подтверждения.");
                break;
        }
    }

    private void OnLicenseConfirmed()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        // Рефлексивный запуск всех подсистем по приоритету
        SubsystemRegistry.InitializeAll();
    }

    private void OnLicenseRejected()
    {
        Log.Error("[CapyLib] 🚨 ВНИМАНИЕ: Лицензия была отозвана или заблокирована! Экстренное отключение всех модулей CapyLib.");
        ShutdownAllSubsystems();
    }

    private void ShutdownAllSubsystems()
    {
        _isInitialized = false;

        // Рефлексивная выгрузка всех подсистем в обратном порядке
        SubsystemRegistry.ShutdownAll();
        SafeExecute("PlayerStateCleaner", PlayerStateCleaner.Shutdown);
    }

    private static void SafeExecute(string name, Action action)
    {
        try
        {
            action();
            Log.Debug($"[CapyLib] Задача '{name}' выполнена.");
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyLib] Ошибка задачи '{name}': {ex}");
        }
    }

    private void EnsureDirectories()
    {
        string baseDir = Path.Combine(Paths.Plugins, "CapyLib");
        string configsDir = Path.Combine(baseDir, "Configs");
        string modulesDir = Path.Combine(baseDir, "Modules");
        string audioDir = Path.Combine(baseDir, "Audio");
        string dbDir = Path.Combine(baseDir, "Database");
        string schematicsDir = Path.Combine(baseDir, "Schematics");

        if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
        if (!Directory.Exists(configsDir)) Directory.CreateDirectory(configsDir);
        if (!Directory.Exists(modulesDir)) Directory.CreateDirectory(modulesDir);
        if (!Directory.Exists(audioDir)) Directory.CreateDirectory(audioDir);
        if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);
        if (!Directory.Exists(schematicsDir)) Directory.CreateDirectory(schematicsDir);
    }

    private string GetOrCreateLicenseKey()
    {
        if (!string.IsNullOrWhiteSpace(Config.LicenseKey))
        {
            return Config.LicenseKey.Trim();
        }

        string keyPath = Path.Combine(Paths.Plugins, "CapyLib", "license.key");
        if (File.Exists(keyPath))
        {
            return File.ReadAllText(keyPath).Trim();
        }

        return string.Empty;
    }
}
