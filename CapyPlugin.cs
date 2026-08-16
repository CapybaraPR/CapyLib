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
public class CapyPlugin : Plugin<CapyConfig>
{
    public override string Name => "CapyLib";
    public override string Author => "CapybaraPR";
    public override string Prefix => "capylib";
    public override Version Version => new(1, 0, 0);

    public static CapyPlugin? Instance { get; private set; }

    public ModuleLoader Loader { get; private set; } = null!;
    public IDatabaseProvider Database { get; private set; } = null!;

    public override void OnEnabled()
    {
        Instance = this;

        // Создание базовых каталогов
        EnsureDirectories();

        // 1. Инициализация DRM лицензии
        string licenseKey = GetOrCreateLicenseKey();
        _ = InitializeWithLicenseAsync(licenseKey);

        base.OnEnabled();
    }

    private async Task InitializeWithLicenseAsync(string licenseKey)
    {
        Log.Info("[CapyLib] Проверка лицензии сервера...");
        bool isValid = await LicenseManager.VerifyAsync(Config.LicenseServerUrl, licenseKey);

        if (!isValid)
        {
            Log.Error("[CapyLib:DRM] Ошибка проверки лицензии! Запуск защиты...");
            LicenseManager.EnforceProtection();
            return;
        }

        // Запуск фонового цикла проверки лицензии
        LicenseManager.StartLicenseLoop(Config.LicenseServerUrl, licenseKey, Config.LicenseCheckIntervalSeconds);

        // 2. Инициализация базы данных
        InitializeDatabase();

        // 3. Инициализация плейсхолдеров
        PlaceholderReplacer.RegisterDefaults();

        // 4. Регистрация аудиоклипов
        AudioRegistry.RegisterClips();

        // 5. Инициализация и запуск загрузчика модулей
        Loader = new ModuleLoader();
        await Loader.InitializeAsync();

        // 6. Вывод фирменного логотипа Капибары в консоль
        PrintCapybaraLogo();
    }

    private void InitializeDatabase()
    {
        try
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
        catch (Exception ex)
        {
            Log.Error($"[CapyLib:Database] Ошибка инициализации БД: {ex.Message}");
        }
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

        File.WriteAllText(keyPath, "YOUR_LICENSE_KEY_HERE");
        return "YOUR_LICENSE_KEY_HERE";
    }

    public override void OnDisabled()
    {
        LicenseManager.Stop();

        CustomItemsManager.UnregisterAll();
        CustomRolesManager.UnregisterAll();

        EventRegistrar.UnregisterAll();
        EventBus.Clear();

        Loader?.ShutdownAsync().GetAwaiter().GetResult();
        Database?.Shutdown();

        Instance = null;
        base.OnDisabled();
    }

    private void PrintCapybaraLogo()
    {
        Log.Info("\n" +
            "===============================================================================\n" +
            "        (\\_/)\n" +
            "       ( •_•)    ██████╗  █████╗ ██████╗ ██╗   ██╗██╗     ██╗██████╗\n" +
            "      / >🍊     ██╔════╝ ██╔══██╗██╔══██╗╚██╗ ██╔╝██║     ██║██╔══██╗\n" +
            "     /     \\    ██║      ███████║██████╔╝ ╚████╔╝ ██║     ██║██████╔╝\n" +
            "    (_______)   ██║      ██╔══██║██╔═══╝   ╚██╔╝  ██║     ██║██╔══██╗\n" +
            "                ╚██████╗ ██║  ██║██║        ██║   ███████╗██║██████╔╝\n" +
            "                 ╚═════╝ ╚═╝  ╚═╝╚═╝        ╚═╝   ╚══════╝╚═╝╚═════╝\n" +
            "\n" +
            $"          [ Capybara Framework & Library v{Version} for SCP:SL EXILED ]\n" +
            $"                    Licensed to: {LicenseManager.LicenseOwner}\n" +
            "===============================================================================");
    }
}
