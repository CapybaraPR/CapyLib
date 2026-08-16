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

        // 1. Поиск внутренних модулей в текущих сборках AppDomain
        LoadInternalModules();

        // 2. Поиск внешних DLL модулей из папки Plugins/CapyLib/Modules
        LoadExternalModules();

        // 3. Топологическая сортировка по графу зависимостей [DependsOn]
        var sortedModules = SortByDependencies(_modules);

        // 4. Последовательный запуск модулей
        foreach (var module in sortedModules)
        {
            if (!module.IsEnabled) continue;

            try
            {
                module.Log.Info($"Активация модуля {module.Name} v{module.Version}...");
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

    private List<ICapyModule> SortByDependencies(List<ICapyModule> modules)
    {
        var result = new List<ICapyModule>();
        var visited = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var moduleMap = modules.ToDictionary(m => m.Name, m => m, StringComparer.OrdinalIgnoreCase);

        void Dfs(ICapyModule current)
        {
            if (visited.TryGetValue(current.Name, out var inProgress))
            {
                if (inProgress)
                {
                    Log.Warn($"[ModuleLoader] Обнаружена циклическая зависимость для модуля '{current.Name}'!");
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
                else
                {
                    Log.Warn($"[ModuleLoader] Модуль '{current.Name}' требует отсутствующий модуль '{depName}'!");
                }
            }

            visited[current.Name] = false;
            result.Add(current);
        }

        foreach (var module in modules)
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
