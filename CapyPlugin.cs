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
/// Главный плагин и точка входа библиотеки CapyLib в среду EXILED.
/// </summary>
public sealed class CapyPlugin : Plugin<CapyConfig>
{
    public override string Name => "CapyLib";
    public override string Author => "CapybaraPR";
    public override string Prefix => "capylib";
    public override Version Version => new(1, 4, 0);

    public static CapyPlugin Instance { get; private set; } = null!;

    public ModuleLoader Loader { get; private set; } = null!;
    public IDatabaseProvider Database { get; private set; } = null!;
    private HarmonyLib.Harmony? _harmony;
    private bool _licenseBlocked;
    private bool _modulesDeferred;

    public override void OnEnabled()
    {
        Instance = this;

        SafeExecute("Directories", EnsureDirectories);
        SafeExecute("PlayerStateCleaner", PlayerStateCleaner.EnsureSubscribed);

        SafeExecute("HarmonyPatches", () =>
        {
            _harmony = new HarmonyLib.Harmony($"capylib.patches.{DateTime.UtcNow.Ticks}");
            _harmony.PatchAll();
            Log.Info("[CapyLib] Harmony-патчи успешно применены.");
        });

        SafeExecute("LicenseCheck", RunLicenseActivation);

        if (_licenseBlocked)
        {
            Log.Error("[CapyLib] Загрузка подсистем CapyLib отменена: лицензия не действительна.");
            base.OnEnabled();
            return;
        }

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

            Exiled.Events.Handlers.Server.RoundStarted += Capy.Engine.Studio.Core.MapManager.OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound += Capy.Engine.Studio.Core.MapManager.OnRoundRestarted;
        });

        if (!_modulesDeferred)
            SafeExecute("ModuleLoader", InitializeModules);

        SafeExecute("ServerBanner", ServerBanner.Show);

        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        SafeExecute("HarmonyUnpatch", () =>
        {
            _harmony?.UnpatchAll(_harmony.Id);
            _harmony = null;
        });

        SafeExecute("LicenseStop", () =>
        {
            LicenseManager.LicenseConfirmed -= OnLicenseConfirmed;
            LicenseManager.Stop();
        });
        SafeExecute("CapyStudio.Disable", () =>
        {
            Exiled.Events.Handlers.Server.RoundStarted -= Capy.Engine.Studio.Core.MapManager.OnRoundStarted;
            Exiled.Events.Handlers.Server.RestartingRound -= Capy.Engine.Studio.Core.MapManager.OnRoundRestarted;

            Capy.Engine.Studio.ToolGun.CapyToolGun.Unregister();
            Capy.Engine.Studio.Core.MapManager.ClearCurrentMap();
            Capy.Engine.Studio.Core.SchematicLoader.DestroyAll();
        });
        SafeExecute("AssKeybinds.Unregister", Capy.Engine.ServerSpecific.AssKeybinds.Unregister);
        SafeExecute("HudModule.Disable", Capy.Engine.Hud.HudModule.Disable);
        SafeExecute("PlayerStateCleaner", PlayerStateCleaner.Shutdown);
        SafeExecute("CustomRoles.Disable", CustomRolesManager.UnregisterAll);
        SafeExecute("CustomItems.Disable", CustomItemsManager.UnregisterAll);
        SafeExecute("EventHandlers.Disable", EventRegistrar.UnregisterAll);
        SafeExecute("AudioRegistry.Unload", AudioRegistry.UnloadAll);
        SafeExecute("EventBus.Clear", EventBus.Clear);

        SafeExecute("ModuleLoader.Shutdown", () => Loader?.ShutdownAsync().Wait());
        SafeExecute("ModuleManager.Clear", ModuleManager.Clear);
        SafeExecute("Database.Shutdown", () => Database?.Shutdown());

        Instance = null!;
        base.OnDisabled();
    }

    private static void SafeExecute(string name, Action action)
    {
        try
        {
            action();
            Log.Debug($"[CapyLib] Подсистема '{name}' успешно инициализирована.");
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyLib] Ошибка инициализации подсистемы '{name}': {ex}");
        }
    }

    private void RunLicenseActivation()
    {
        LicenseManager.LicenseConfirmed += OnLicenseConfirmed;

        LicenseActivation activation = LicenseManager
            .ActivateAsync(Config.LicenseServerUrl, GetOrCreateLicenseKey(), Config.LicenseCheckIntervalSeconds)
            .GetAwaiter().GetResult();

        switch (activation)
        {
            case LicenseActivation.Blocked:
                _licenseBlocked = true;
                break;

            case LicenseActivation.Deferred:
                _modulesDeferred = true;
                break;
        }
    }

    private void OnLicenseConfirmed()
    {
        LicenseManager.LicenseConfirmed -= OnLicenseConfirmed;

        SafeExecute("ModuleLoader", InitializeModules);
        SafeExecute("ServerBanner", ServerBanner.Show);
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
            return Config.LicenseKey;
        }

        string keyPath = Path.Combine(Paths.Plugins, "CapyLib", "license.key");
        if (File.Exists(keyPath))
        {
            return File.ReadAllText(keyPath).Trim();
        }

        File.WriteAllText(keyPath, "DEV_LICENSE");
        return "DEV_LICENSE";
    }
}
