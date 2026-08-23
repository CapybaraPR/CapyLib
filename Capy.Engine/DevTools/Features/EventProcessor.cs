using System;
using System.Collections.Generic;
using System.Reflection;
using Exiled.Events.Features;
using Exiled.API.Features;
using Handlers = Exiled.Events.Handlers;

namespace Capy.Engine.DevTools.Features;

public static class EventProcessor {
    private static readonly Dictionary<Type, EventInfo> Events = [];
    private static readonly Dictionary<Type, List<Delegate>> HandlersList = [];
    private static readonly Dictionary<Delegate, Delegate> Kys = [];

    internal static void CollectEventsData() {
        List<Type> eventTypes =
        [
            typeof(Handlers.Player),
            typeof(Handlers.Server),
            typeof(Handlers.Map),
            typeof(Handlers.Warhead),
            typeof(Handlers.Scp049),
            typeof(Handlers.Scp079),
            typeof(Handlers.Scp096),
            typeof(Handlers.Scp106),
            typeof(Handlers.Scp173),
            typeof(Handlers.Scp3114),
            typeof(Handlers.Scp914),
            typeof(Handlers.Scp939)
        ];

        foreach (Type eType in eventTypes) {
            EventInfo[] events = eType.GetEvents();

            foreach (EventInfo ev in events) {
                MethodInfo add = ev.GetAddMethod(true);

                bool isStatic = add != null && add.IsStatic;

                if (!isStatic)
                    continue;

                Type handlerType = ev.EventHandlerType;

                MethodInfo invoke = handlerType.GetMethod("Invoke");
                ParameterInfo[] parameters = invoke.GetParameters();

                if (parameters.Length != 1)
                    continue;

                Events[parameters[0].ParameterType] = ev;
            }
        }
    }

    public static void RegisterEvent<TArgs>(
        CustomEventHandler<TArgs> handler,
        Func<TArgs, bool> condition)
        where TArgs : EventArgs {
        Type argsType = typeof(TArgs);

        if (!HandlersList.ContainsKey(argsType))
            HandlersList[argsType] = [];

        if (HandlersList[argsType].Contains(handler))
            return;

        if (!Events.TryGetValue(argsType, out EventInfo ev)) {
            Log.Warn($"Event for type {argsType.Name} not found");
            return;
        }

        CustomEventHandler<TArgs> handlerMethod = a => {
            if (condition(a))
                handler(a);
        };

        ev.AddEventHandler(null, handlerMethod);
        HandlersList[argsType].Add(handler);
        Kys[handler] = handlerMethod;
    }

    public static void UnregsiterEvent<TArgs>(Delegate handler) {
        Type argsType = typeof(TArgs);

        if (!HandlersList.TryGetValue(argsType, out List<Delegate> handlers) || !handlers.Contains(handler))
            return;

        if (!Events.TryGetValue(argsType, out EventInfo ev))
            return;

        ev.RemoveEventHandler(null, Kys[handler]);
        handlers.Remove(handler);
        Kys.Remove(handler);
    }
}