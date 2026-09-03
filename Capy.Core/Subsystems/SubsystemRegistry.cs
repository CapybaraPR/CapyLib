using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Exiled.API.Features;

namespace Capy.Core.Subsystems;

/// <summary>
/// Реестр и диспетчер подсистем ядра CapyLib на базе рефлексии.
/// Автоматически обнаруживает все реализации ICapySubsystem, сортирует по Priority и безопасно управляет их жизненным циклом.
/// </summary>
public static class SubsystemRegistry
{
    private static readonly List<ICapySubsystem> _subsystems = new();
    private static readonly List<ICapySubsystem> _initializedSubsystems = new();
    private static readonly object _lock = new();

    public static IReadOnlyList<ICapySubsystem> Subsystems
    {
        get
        {
            lock (_lock)
            {
                return _subsystems.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Автоматически сканирует текущую и подключенные сборки на наличие ICapySubsystem и регистрирует их.
    /// </summary>
    public static void DiscoverSubsystems()
    {
        lock (_lock)
        {
            _subsystems.Clear();

            var discoveredTypes = new HashSet<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => typeof(ICapySubsystem).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in types)
                    {
                        if (discoveredTypes.Add(type))
                        {
                            try
                            {
                                if (Activator.CreateInstance(type) is ICapySubsystem subsystem)
                                {
                                    _subsystems.Add(subsystem);
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[SubsystemRegistry] Не удалось создать экземпляр подсистемы {type.FullName}: {ex.Message}");
                            }
                        }
                    }
                }
                catch
                {
                    // Пропускаем сборки без доступа к типам
                }
            }

            // Сортировка по приоритету (меньший приоритет выполняется раньше)
            _subsystems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            Log.Debug($"[SubsystemRegistry] Обнаружено подсистем: {_subsystems.Count}. Порядок запуска: {string.Join(" -> ", _subsystems.Select(s => s.Name))}");
        }
    }

    /// <summary>
    /// Запуск всех зарегистрированных подсистем по цепочке приоритетов.
    /// </summary>
    public static void InitializeAll()
    {
        lock (_lock)
        {
            if (_subsystems.Count == 0)
            {
                DiscoverSubsystems();
            }

            _initializedSubsystems.Clear();

            foreach (var subsystem in _subsystems)
            {
                try
                {
                    Log.Debug($"[SubsystemRegistry] Запуск подсистемы '{subsystem.Name}' (приоритет: {subsystem.Priority})...");
                    subsystem.Initialize();
                    _initializedSubsystems.Add(subsystem);
                }
                catch (Exception ex)
                {
                    Log.Error($"[SubsystemRegistry] Ошибка при запуске подсистемы '{subsystem.Name}': {ex}");
                }
            }

            Log.Debug($"[Subsystems] Запущенные подсистемы: {string.Join(", ", _initializedSubsystems.Select(s => s.Name))}.");
            var moduleLoaderSub = GetSubsystem<ModuleLoaderSubsystem>();
            var moduleNames = moduleLoaderSub?.Loader.Modules.Select(m => m.Name).ToList() ?? new List<string>();
            string modStr = moduleNames.Count > 0 ? $", модули: {string.Join(", ", moduleNames)}" : "";
            Log.Info($"[CapyLib] Ядро успешно запущено ({_initializedSubsystems.Count} подсистем{modStr}).");
        }
    }

    /// <summary>
    /// Остановка всех активных подсистем в обратном порядке (LIFO).
    /// </summary>
    public static void ShutdownAll()
    {
        lock (_lock)
        {
            // Выгружаем в обратном порядке приоритетов
            for (int i = _initializedSubsystems.Count - 1; i >= 0; i--)
            {
                var subsystem = _initializedSubsystems[i];
                try
                {
                    Log.Debug($"[SubsystemRegistry] Остановка подсистемы '{subsystem.Name}'...");
                    subsystem.Shutdown();
                }
                catch (Exception ex)
                {
                    Log.Error($"[SubsystemRegistry] Ошибка при остановке подсистемы '{subsystem.Name}': {ex.Message}");
                }
            }

            _initializedSubsystems.Clear();
            Log.Info("[SubsystemRegistry] Все подсистемы ядра CapyLib успешно выгружены.");
        }
    }

    /// <summary>
    /// Получить конкретную подсистему по типу.
    /// </summary>
    public static T? GetSubsystem<T>() where T : class, ICapySubsystem
    {
        lock (_lock)
        {
            return _subsystems.OfType<T>().FirstOrDefault();
        }
    }
}
