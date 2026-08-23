using Capy.Core.API;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Capy.Core.Loader;

/// <summary>
/// Мульти-файловый менеджер YAML конфигураций для модулей CapyLib.
/// </summary>
public static class ConfigLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static string ConfigDirectory => Path.Combine(Paths.Configs, "CapyLib");

    private static void EnsureDirectoryExists()
    {
        if (!Directory.Exists(ConfigDirectory))
        {
            Directory.CreateDirectory(ConfigDirectory);
        }
    }

    private static string SanitizeModuleName(string moduleName)
    {
        string safe = (moduleName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(safe))
            return "UnnamedModule";

        foreach (char c in Path.GetInvalidFileNameChars())
            safe = safe.Replace(c, '_');

        return safe;
    }

    private static string GetConfigFilePath(string moduleName)
    {
        moduleName = SanitizeModuleName(moduleName);

        try
        {
            ushort port = Exiled.API.Features.Server.Port;
            if (port > 0)
            {
                string portSpecific = Path.Combine(ConfigDirectory, $"{moduleName}_{port}.yml");
                if (File.Exists(portSpecific))
                    return portSpecific;
            }
        }
        catch
        {
        }

        string standard = Path.Combine(ConfigDirectory, $"{moduleName}.yml");
        if (File.Exists(standard))
            return standard;

        string legacy = Path.Combine(Paths.Plugins, "CapyLib", "Configs", $"{moduleName}.yml");
        if (File.Exists(legacy))
            return legacy;

        try
        {
            ushort port = Exiled.API.Features.Server.Port;
            if (port > 0)
                return Path.Combine(ConfigDirectory, $"{moduleName}_{port}.yml");
        }
        catch
        {
        }

        return standard;
    }

    public static void ProcessConfig(ICapyModule module)
    {
        if (module == null) return;
        EnsureDirectoryExists();

        var configType = module.GetConfigType();
        var filePath = GetConfigFilePath(module.Name);

        try
        {
            if (!File.Exists(filePath))
            {
                var defaultConfig = Activator.CreateInstance(configType);
                var yaml = Serializer.Serialize(defaultConfig);
                File.WriteAllText(filePath, yaml);
                module.SetConfig(defaultConfig);
                return;
            }

            var fileContent = File.ReadAllText(filePath);
            var parsedConfig = Deserializer.Deserialize(fileContent, configType);

            if (parsedConfig != null)
            {
                module.SetConfig(parsedConfig);
            }
            else
            {
                var fallback = Activator.CreateInstance(configType);
                module.SetConfig(fallback);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[ConfigLoader] Ошибка при загрузке конфига '{module.Name}': {ex.Message}");
            var fallback = Activator.CreateInstance(configType);
            module.SetConfig(fallback);
        }
    }

    public static void SaveConfig(ICapyModule module, object config)
    {
        if (module == null || config == null) return;
        EnsureDirectoryExists();

        var filePath = GetConfigFilePath(module.Name);
        try
        {
            var yaml = Serializer.Serialize(config);
            File.WriteAllText(filePath, yaml);
        }
        catch (Exception ex)
        {
            Log.Error($"[ConfigLoader] Ошибка при сохранении конфига '{module.Name}': {ex.Message}");
        }
    }
}
