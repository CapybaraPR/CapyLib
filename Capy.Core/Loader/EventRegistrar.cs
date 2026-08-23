using System.Reflection;
using Capy.Core.API.Attributes;

namespace Capy.Core.Loader;

public static class EventRegistrar
{
    private static readonly object Sync = new();
    private static readonly List<(Type Owner, EventInfo Event, Delegate Handler)> SubscribedHandlers = new();

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
                if (!method.IsStatic) continue;
                if (method.GetCustomAttribute<CapyEventHandlerAttribute>() == null) continue;

                SubscribeMethod(managerType, method, exiledHandlerTypes);
            }
        }
    }

    private static void SubscribeMethod(Type owner, MethodInfo method, List<Type> exiledHandlerTypes)
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
                        TrySubscribe(owner, evt, method);
                    }
                }
            }
        }
        else if (parameters.Length == 0 &&
                 (method.Name == "OnRoundStarted" || method.Name == "OnRoundRestarted" || method.Name == "OnRestartingRound"))
        {
            string searchEventName = method.Name.StartsWith("On") ? method.Name.Substring(2) : method.Name;

            foreach (var handlerType in exiledHandlerTypes)
            {
                var evt = handlerType.GetEvent(searchEventName, BindingFlags.Public | BindingFlags.Static);
                if (evt != null)
                {
                    TrySubscribe(owner, evt, method);
                }
            }
        }
    }

    private static void TrySubscribe(Type owner, EventInfo evt, MethodInfo method)
    {
        try
        {
            Delegate handler = Delegate.CreateDelegate(evt.EventHandlerType!, method);
            evt.AddEventHandler(null, handler);

            lock (Sync)
            {
                SubscribedHandlers.Add((owner, evt, handler));
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[EventRegistrar] Ошибка подписки {owner.Name}.{method.Name} на {evt.Name}: {ex.Message}");
        }
    }

    public static void Unregister(Type ownerType)
    {
        List<(Type, EventInfo, Delegate)> victims;

        lock (Sync)
        {
            victims = SubscribedHandlers.Where(h => h.Owner == ownerType).ToList();
            foreach (var victim in victims)
                SubscribedHandlers.Remove(victim);
        }

        int count = 0;
        foreach (var (_, evt, handler) in victims)
        {
            try
            {
                evt.RemoveEventHandler(null, handler);
                count++;
            }
            catch { }
        }

        if (count > 0)
            Log.Debug($"[EventRegistrar] Отписано {count} обработчиков типа {ownerType.Name}.");
    }

    public static void UnregisterAll()
    {
        List<(Type, EventInfo, Delegate)> all;

        lock (Sync)
        {
            all = SubscribedHandlers.ToList();
            SubscribedHandlers.Clear();
        }

        int count = 0;
        foreach (var (_, evt, handler) in all)
        {
            try
            {
                evt.RemoveEventHandler(null, handler);
                count++;
            }
            catch { }
        }

        Log.Debug($"[EventRegistrar] Отписано {count} обработчиков событий.");
    }
}
