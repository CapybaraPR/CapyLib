using CommandSystem;
using Capy.Core.API;

namespace Capy.Core.Loader;

/// <summary>
/// Информация о зарегистрированном модуле или плагине.
/// </summary>
public class CapyModuleInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public ICapyModule? ModuleInstance { get; set; }
    public Action? EnableAction { get; set; }
    public Action? DisableAction { get; set; }
}

/// <summary>
/// Рантайм-менеджер управления модулями, подсистемами и плагинами CapyLib.
/// </summary>
public static class ModuleManager
{
    private static readonly Dictionary<string, CapyModuleInfo> Modules = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Проверка прав доступа: серверная консоль или авторизованные SteamID из конфига.
    /// </summary>
    public static bool CheckAccess(ICommandSender sender)
    {
        if (sender is ServerConsoleSender) return true;

        var player = Player.Get(sender);
        if (player != null)
        {
            var authorizedList = CapyPlugin.Instance?.Config.AuthorizedSteamIds;
            if (authorizedList != null)
            {
                if (authorizedList.Contains(player.UserId) || authorizedList.Contains(player.RawUserId))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static void Register(ICapyModule module)
    {
        if (module == null) return;

        Modules[module.Name] = new CapyModuleInfo
        {
            Name = module.Name,
            DisplayName = module.Name,
            Description = $"Модуль {module.Name} v{module.Version} by {module.Author}",
            IsEnabled = module.IsEnabled,
            ModuleInstance = module,
            EnableAction = () =>
            {
                module.OnEnabled();
                module.IsEnabled = true;
            },
            DisableAction = () =>
            {
                module.OnDisabled();
                module.IsEnabled = false;
            }
        };
    }

    public static void RegisterCustom(CapyModuleInfo info)
    {
        if (info != null)
        {
            Modules[info.Name] = info;
        }
    }

    public static IReadOnlyCollection<CapyModuleInfo> GetAll() => Modules.Values;

    public static bool TryGet(string name, out CapyModuleInfo? module)
    {
        return Modules.TryGetValue(name, out module);
    }

    public static bool Enable(string name, out string message)
    {
        if (!TryGet(name, out var module) || module == null)
        {
            message = $"Модуль '{name}' не найден в CapyLib.";
            return false;
        }

        if (module.IsEnabled)
        {
            message = $"Модуль '{module.Name}' уже активен.";
            return false;
        }

        try
        {
            module.EnableAction?.Invoke();
            module.IsEnabled = true;
            message = $"Модуль '{module.Name}' успешно активирован!";
            Log.Info($"[CapyLib:ModuleManager] {message}");
            return true;
        }
        catch (Exception ex)
        {
            message = $"Ошибка при активации '{module.Name}': {ex.Message}";
            Log.Error(message);
            return false;
        }
    }

    public static bool Disable(string name, out string message)
    {
        if (!TryGet(name, out var module) || module == null)
        {
            message = $"Модуль '{name}' не найден в CapyLib.";
            return false;
        }

        if (!module.IsEnabled)
        {
            message = $"Модуль '{module.Name}' уже отключен.";
            return false;
        }

        try
        {
            module.DisableAction?.Invoke();
            module.IsEnabled = false;
            message = $"Модуль '{module.Name}' успешно отключен!";
            Log.Info($"[CapyLib:ModuleManager] {message}");
            return true;
        }
        catch (Exception ex)
        {
            message = $"Ошибка при отключении '{module.Name}': {ex.Message}";
            Log.Error(message);
            return false;
        }
    }

    public static bool Toggle(string name, out string message)
    {
        if (!TryGet(name, out var module) || module == null)
        {
            message = $"Модуль '{name}' не найден.";
            return false;
        }

        return module.IsEnabled ? Disable(name, out message) : Enable(name, out message);
    }

    public static bool Restart(string name, out string message)
    {
        if (!TryGet(name, out var module) || module == null)
        {
            message = $"Модуль '{name}' не найден.";
            return false;
        }

        try
        {
            if (module.IsEnabled)
            {
                module.DisableAction?.Invoke();
            }

            if (module.ModuleInstance != null)
            {
                ConfigLoader.ProcessConfig(module.ModuleInstance);
            }

            module.EnableAction?.Invoke();
            module.IsEnabled = true;
            message = $"Модуль '{module.Name}' успешно перезапущен!";
            Log.Info($"[CapyLib:ModuleManager] {message}");
            return true;
        }
        catch (Exception ex)
        {
            message = $"Ошибка при перезапуске '{module.Name}': {ex.Message}";
            Log.Error(message);
            return false;
        }
    }

    public static void DisableAll()
    {
        foreach (var module in Modules.Values)
        {
            if (module.IsEnabled)
            {
                try
                {
                    module.DisableAction?.Invoke();
                    module.IsEnabled = false;
                }
                catch { }
            }
        }
    }

    public static void Clear()
    {
        Modules.Clear();
    }

    public static bool ReloadAllConfigs(out string message)
    {
        try
        {
            foreach (var module in Modules.Values)
            {
                if (module.ModuleInstance != null)
                {
                    ConfigLoader.ProcessConfig(module.ModuleInstance);
                }
            }
            message = "Все YAML-конфигурации модулей CapyLib успешно перезагружены!";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Ошибка перезагрузки: {ex.Message}";
            return false;
        }
    }
}
