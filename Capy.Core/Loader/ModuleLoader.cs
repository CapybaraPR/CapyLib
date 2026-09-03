using System.Reflection;
using Capy.Core.API;
using Capy.Core.API.Attributes;
using Capy.Core.Services;

namespace Capy.Core.Loader;

/// <summary>
/// Загрузчик и диспетчер жизненного цикла модулей с поддержкой графа зависимостей.
/// </summary>
public class ModuleLoader
{
    private readonly List<ICapyModule> _modules = new();

    public IReadOnlyList<ICapyModule> Modules => _modules.AsReadOnly();

    public async Task InitializeAsync()
    {
        _modules.Clear();

        LoadInternalModules();
        LoadExternalModules();

        var unresolvable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sortedModules = SortByDependencies(_modules, unresolvable);

        foreach (var module in sortedModules)
        {
            if (!module.IsEnabled || unresolvable.Contains(module.Name)) continue;

            try
            {
                module.Log.Debug($"Активация модуля {module.Name} v{module.Version}...");
                await module.OnEnabledAsync();
                ModuleManager.Register(module);
            }
            catch (Exception ex)
            {
                module.Log.Error($"Ошибка при запуске модуля: {ex}");
            }
        }
    }

    private void LoadInternalModules()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(ICapyModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in moduleTypes)
                {
                    InstantiateAndPrepare(type);
                }
            }
            catch
            {
                // Пропускаем сборки без доступа к типам
            }
        }
    }

    private void LoadExternalModules()
    {
        var modulesDir = Path.Combine(Paths.Plugins, "CapyLib", "Modules");
        if (!Directory.Exists(modulesDir))
        {
            Directory.CreateDirectory(modulesDir);
            return;
        }

        foreach (var dllPath in Directory.GetFiles(modulesDir, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(ICapyModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in moduleTypes)
                {
                    InstantiateAndPrepare(type);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[ModuleLoader] Ошибка при загрузке внешнего модуля из {dllPath}: {ex.Message}");
            }
        }
    }

    private void InstantiateAndPrepare(Type type)
    {
        if (_modules.Any(m => m.GetType() == type)) return;

        try
        {
            if (Activator.CreateInstance(type) is ICapyModule module)
            {
                module.Log = new CapyLogger(module.Name, CapyPlugin.Instance?.Config.Debug ?? false);
                ConfigLoader.ProcessConfig(module);
                _modules.Add(module);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[ModuleLoader] Не удалось создать экземпляр {type.FullName}: {ex.Message}");
        }
    }

    private List<ICapyModule> SortByDependencies(List<ICapyModule> modules, HashSet<string> unresolvable)
    {
        var result = new List<ICapyModule>();
        var visited = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var moduleMap = new Dictionary<string, ICapyModule>(StringComparer.OrdinalIgnoreCase);

        foreach (var module in modules)
        {
            if (string.IsNullOrWhiteSpace(module.Name))
            {
                Log.Error($"[ModuleLoader] Модуль {module.GetType().FullName} имеет пустое имя и будет пропущен.");
                continue;
            }

            if (moduleMap.ContainsKey(module.Name))
            {
                Log.Error($"[ModuleLoader] Дубликат имени модуля '{module.Name}' ({module.GetType().FullName}). Дубликат пропущен.");
                continue;
            }

            moduleMap[module.Name] = module;
        }

        void Dfs(ICapyModule current)
        {
            if (visited.TryGetValue(current.Name, out var inProgress))
            {
                if (inProgress && unresolvable.Add(current.Name))
                {
                    Log.Error($"[ModuleLoader] Циклическая зависимость: модуль '{current.Name}' не будет запущен.");
                }
                return;
            }

            visited[current.Name] = true;

            var dependencies = current.GetType()
                .GetCustomAttributes<DependsOnAttribute>()
                .Select(attr => attr.TargetModuleName);

            foreach (var depName in dependencies)
            {
                if (moduleMap.TryGetValue(depName, out var depModule))
                {
                    Dfs(depModule);
                }
                else if (unresolvable.Add(current.Name))
                {
                    Log.Error($"[ModuleLoader] Модуль '{current.Name}' требует отсутствующий модуль '{depName}' и не будет запущен.");
                }

                if (unresolvable.Contains(depName) && !unresolvable.Contains(current.Name))
                {
                    unresolvable.Add(current.Name);
                    Log.Error($"[ModuleLoader] Модуль '{current.Name}' отключён: зависимость '{depName}' неразрешима.");
                }
            }

            visited[current.Name] = false;

            if (!unresolvable.Contains(current.Name))
                result.Add(current);
        }

        foreach (var module in moduleMap.Values)
        {
            if (!visited.ContainsKey(module.Name))
            {
                Dfs(module);
            }
        }

        return result;
    }

    public async Task ShutdownAsync()
    {
        for (int i = _modules.Count - 1; i >= 0; i--)
        {
            var module = _modules[i];
            if (module.IsEnabled)
            {
                try
                {
                    module.Log.Info($"Остановка модуля {module.Name}...");
                    await module.OnDisabledAsync();
                    module.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    module.Log.Error($"Ошибка при остановке модуля: {ex}");
                }
            }
        }
        _modules.Clear();
    }
}
