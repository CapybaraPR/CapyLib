using Capy.Core.Database;
using Capy.Core.DRM;
using Capy.Core.Loader;
using Capy.Core.Services;
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
    public override Version Version => new(1, 2, 0);

    public static CapyPlugin Instance { get; private set; } = null!;

    public ModuleLoader Loader { get; private set; } = null!;
    public IDatabaseProvider Database { get; private set; } = null!;
    private HarmonyLib.Harmony? _harmony;

    public override void OnEnabled()
    {
        Instance = this;

        // Создание базовых каталогов
        SafeExecute("Directories", EnsureDirectories);

        // 0. Harmony Patches
        SafeExecute("HarmonyPatches", () =>
        {
            _harmony = new HarmonyLib.Harmony($"capylib.patches.{DateTime.UtcNow.Ticks}");
            _harmony.PatchAll();
            Log.Info("[CapyLib] Harmony-патчи успешно применены.");
        });

        // 1. Инициализация DRM лицензии
        string licenseKey = GetOrCreateLicenseKey();
        SafeExecute("LicenseCheck", () =>
        {
            var task = LicenseManager.VerifyAsync(Config.LicenseServerUrl, licenseKey);
            task.Wait();
            if (!task.Result)
            {
                LicenseManager.EnforceProtection();
            }
            else
            {
                LicenseManager.StartLicenseLoop(Config.LicenseServerUrl, licenseKey, Config.LicenseCheckIntervalSeconds);
            }
        });

        // 2. Инициализация базы данных
        SafeExecute("Database", InitializeDatabase);

        // 3. Инициализация плейсхолдеров
        SafeExecute("Placeholders", PlaceholderReplacer.RegisterDefaults);

        // 4. Регистрация аудиоклипов
        SafeExecute("AudioRegistry", AudioRegistry.RegisterClips);

        // 5. Загрузка модулей
        SafeExecute("ModuleLoader", () =>
        {
            Loader = new ModuleLoader();
            Loader.InitializeAsync().Wait();
        });

        // 6. Вывод загрузочного баннера в стиле AspectLib
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

        SafeExecute("LicenseStop", LicenseManager.Stop);
        SafeExecute("CustomRoles.Disable", CustomRolesManager.UnregisterAll);
        SafeExecute("CustomItems.Disable", CustomItemsManager.UnregisterAll);
        SafeExecute("EventHandlers.Disable", EventRegistrar.UnregisterAll);
        SafeExecute("EventBus.Clear", EventBus.Clear);

        SafeExecute("ModuleLoader.Shutdown", () => Loader?.ShutdownAsync().Wait());
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
            Log.Error($"[CapyLib] Ошибка инициализации подсистемы '{name}': {ex.Message}");
        }
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
