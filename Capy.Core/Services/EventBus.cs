namespace Capy.Core.Services;

/// <summary>
/// Потокобезопасная Pub/Sub шина сообщений для взаимодействия между модулями и плагинами.
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> Handlers = new();
    private static readonly object LockObj = new();

    public static void Subscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null) return;
        lock (LockObj)
        {
            var type = typeof(TEvent);
            if (!Handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                Handlers[type] = list;
            }
            list.Add(handler);
        }
    }

    public static void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (handler == null) return;
        lock (LockObj)
        {
            var type = typeof(TEvent);
            if (Handlers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    Handlers.Remove(type);
                }
            }
        }
    }

    public static void Publish<TEvent>(TEvent eventMessage)
    {
        Delegate[]? handlersCopy = null;
        lock (LockObj)
        {
            var type = typeof(TEvent);
            if (Handlers.TryGetValue(type, out var list))
            {
                handlersCopy = list.ToArray();
            }
        }

        if (handlersCopy != null)
        {
            foreach (var handler in handlersCopy)
            {
                try
                {
                    ((Action<TEvent>)handler)(eventMessage);
                }
                catch (Exception ex)
                {
                    Log.Error($"[EventBus] Исключение при обработке события {typeof(TEvent).Name}: {ex}");
                }
            }
        }
    }

    public static void Clear()
    {
        lock (LockObj)
        {
            Handlers.Clear();
        }
    }
}
