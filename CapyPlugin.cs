using Capy.Core.Database;
using Capy.Core.DRM;
using Capy.Core.Loader;
using Capy.Core.Services;
using Capy.Engine;
using Capy.Engine.Audio;
using Capy.Engine.CustomItems.Manager;
using Capy.Engine.CustomRoles.Manager;

namespace Capy;

/// <summary>
/// Главный плагин и точка входа библиотеки CapyLib в среду EXILED с защитой DRM.
/// </summary>
public sealed class CapyPlugin : Plugin<CapyConfig>
{
    public override string Name => "CapyLib";
    public override string Author => "CapybaraPR";
    public override string Prefix => "capylib";
    public override Version Version => new(1, 4, 5);

    public static CapyPlugin Instance { get; private set; } = null!;

    public ModuleLoader Loader { get; private set; } = null!;
    public IDatabaseProvider Database { get; private set; } = null!;
    private HarmonyLib.Harmony? _harmony;
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
        Log.Info($"[CapyLib] 🔓 Лицензия подтверждена! Запуск всех подсистем CapyLib (Владелец: {LicenseManager.LicenseOwner})...");

        SafeExecute("HarmonyPatches", () =>
        {
            _harmony = new HarmonyLib.Harmony($"capylib.patches.{DateTime.UtcNow.Ticks}");
            _harmony.PatchAll();
            Log.Info("[CapyLib] Harmony-патчи успешно применены.");
        });

        SafeExecute("Database", InitializeDatabase);
        SafeExecute("Placeholders", PlaceholderReplacer.RegisterDefaults);
        SafeExecute("AudioRegistry", AudioRegistry.RegisterClips);
        SafeExecute("AssKeybinds", Capy.Engine.ServerSpecific.AssKeybinds.Initialize);
        SafeExecute("HudModule", Capy.Engine.Hud.HudModule.Enable);
        SafeExecute("CapyStudio", () =>
        {
            Capy.Engine.Studio.Core.SchematicLoader.Initialize();
            Capy.Engine.Studio.Core.MapManager.Initialize();
            Capy.Engine.Studio.ToolGun.CapyToolGun.Initialize();
            Exiled.Events.Handlers.Server.WaitingForPlayers += Capy.Engine.Studio.Core.PrefabManager.Initialize;
            Exiled.Events.Handlers.Server.RoundStarted += Capy.Engine.Studio.Core.MapManager.OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound += Capy.Engine.Studio.Core.MapManager.OnRoundRestarted;
        });

        SafeExecute("ModuleLoader", InitializeModules);
        SafeExecute("ServerBanner", ServerBanner.Show);
    }

    private void OnLicenseRejected()
    {
        Log.Error("[CapyLib] 🚨 ВНИМАНИЕ: Лицензия была отозвана или заблокирована! Экстренное отключение всех модулей CapyLib.");
        ShutdownAllSubsystems();
    }

    private void ShutdownAllSubsystems()
    {
        _isInitialized = false;

        SafeExecute("HarmonyUnpatch", () =>
        {
            _harmony?.UnpatchAll(_harmony.Id);
            _harmony = null;
        });

        SafeExecute("CapyStudio.Disable", () =>
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= Capy.Engine.Studio.Core.PrefabManager.Initialize;
            Exiled.Events.Handlers.Server.RoundStarted -= Capy.Engine.Studio.Core.MapManager.OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound -= Capy.Engine.Studio.Core.MapManager.OnRoundRestarted;

            Capy.Engine.Studio.ToolGun.CapyToolGun.Unregister();
            Capy.Engine.Studio.Core.MapManager.ClearCurrentMap();
            Capy.Engine.Studio.Core.SchematicLoader.DestroyAll();
            Capy.Engine.Studio.Core.PrefabManager.Reset();
        });

        SafeExecute("AssKeybinds.Unregister", Capy.Engine.ServerSpecific.AssKeybinds.Unregister);
        SafeExecute("HudModule.Disable", Capy.Engine.Hud.HudModule.Disable);
        SafeExecute("PlayerStateCleaner", PlayerStateCleaner.Shutdown);
        SafeExecute("CustomRoles.Disable", CustomRolesManager.UnregisterAll);
        SafeExecute("CustomItems.Disable", CustomItemsManager.UnregisterAll);
        SafeExecute("EventHandlers.Disable", EventRegistrar.UnregisterAll);
        SafeExecute("AudioRegistry.Unload", AudioRegistry.UnloadAll);
        SafeExecute("EventBus.Clear", EventBus.Clear);

        SafeExecute("ModuleLoader.Shutdown", () => { if (Loader != null) Loader.ShutdownAsync().GetAwaiter().GetResult(); });
        SafeExecute("ModuleManager.Clear", ModuleManager.Clear);
        SafeExecute("Database.Shutdown", () => Database?.Shutdown());
    }

    private static void SafeExecute(string name, Action action)
    {
        try
        {
            action();
            Log.Debug($"[CapyLib] Подсистема '{name}' выполнена.");
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyLib] Ошибка подсистемы '{name}': {ex}");
        }
    }

    private void InitializeModules()
    {
        Loader ??= new ModuleLoader();
        Loader.InitializeAsync().GetAwaiter().GetResult();
    }

    private void InitializeDatabase()
    {
        if (string.Equals(Config.DatabaseType, "MongoDB", StringComparison.OrdinalIgnoreCase))
        {
            Database = new MongoDbProvider(Config.MongoConnectionString, Config.MongoDatabaseName);
        }
        else
        {
            Database = new LiteDbProvider();
        }
        Database.Initialize();
    }

    private void EnsureDirectories()
    {
        string baseDir = Path.Combine(Paths.Plugins, "CapyLib");
        string configsDir = Path.Combine(baseDir, "Configs");
        string modulesDir = Path.Combine(baseDir, "Modules");
        string audioDir = Path.Combine(baseDir, "Audio");
        string dbDir = Path.Combine(baseDir, "Database");

        if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);
        if (!Directory.Exists(configsDir)) Directory.CreateDirectory(configsDir);
        if (!Directory.Exists(modulesDir)) Directory.CreateDirectory(modulesDir);
        if (!Directory.Exists(audioDir)) Directory.CreateDirectory(audioDir);
        if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);
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
