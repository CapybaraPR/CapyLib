using System.Reflection;

namespace Capy.Core.Loader;

/// <summary>
/// Автоматический регистратор обработчиков событий EXILED с использованием рефлексии.
/// </summary>
public static class EventRegistrar
{
    private static readonly List<(EventInfo evt, Delegate handler)> SubscribedHandlers = new();

    public static void Register(params Type[] targetTypes)
    {
        Assembly exiledEventsAssembly = typeof(Exiled.Events.Handlers.Player).Assembly;

        var exiledHandlerTypes = exiledEventsAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("Exiled.Events.Handlers") && t.IsClass && t.IsAbstract && t.IsSealed)
            .ToList();

        foreach (var managerType in targetTypes)
        {
            if (managerType == null) continue;

            var methods = managerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 1)
                {
                    Type eventArgType = parameters[0].ParameterType;

                    foreach (var handlerType in exiledHandlerTypes)
                    {
                        foreach (var evt in handlerType.GetEvents(BindingFlags.Public | BindingFlags.Static))
                        {
                            var invokeMethod = evt.EventHandlerType?.GetMethod("Invoke");
                            var eventParams = invokeMethod?.GetParameters();
                            if (eventParams != null && eventParams.Length == 1 && eventParams[0].ParameterType == eventArgType)
                            {
                                try
                                {
                                    Delegate handler = Delegate.CreateDelegate(evt.EventHandlerType!, method);
                                    evt.AddEventHandler(null, handler);
                                    SubscribedHandlers.Add((evt, handler));
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"[EventRegistrar] Ошибка подписки {managerType.Name}.{method.Name}: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                else if (parameters.Length == 0 && (method.Name == "OnRoundStarted" || method.Name == "OnRoundRestarted" || method.Name == "OnRestartingRound"))
                {
                    string searchEventName = method.Name.StartsWith("On") ? method.Name.Substring(2) : method.Name;
                    foreach (var handlerType in exiledHandlerTypes)
                    {
                        var evt = handlerType.GetEvent(searchEventName, BindingFlags.Public | BindingFlags.Static);
                        if (evt != null)
                        {
                            try
                            {
                                Delegate handler = Delegate.CreateDelegate(evt.EventHandlerType!, method);
                                evt.AddEventHandler(null, handler);
                                SubscribedHandlers.Add((evt, handler));
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[EventRegistrar] Ошибка подписки {managerType.Name}.{method.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }
        }
    }

    public static void UnregisterAll()
    {
        int count = 0;
        foreach (var (evt, handler) in SubscribedHandlers)
        {
            try
            {
                evt.RemoveEventHandler(null, handler);
                count++;
            }
            catch { }
        }
        SubscribedHandlers.Clear();
        Log.Debug($"[EventRegistrar] Отписано {count} обработчиков событий.");
    }
}
