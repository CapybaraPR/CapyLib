using System.Collections.Concurrent;
using Capy.Engine.Hints.Service;
using MEC;

namespace Capy.Engine.Hints.Extensions;

public static class ShowHintExtensions
{
    private static readonly ConcurrentDictionary<int, Dictionary<string, AbstractHint>> TaggedHints = new();
    private static readonly ConcurrentDictionary<int, Dictionary<string, CoroutineHandle>> ExpiryHandles = new();

    public static void ShowCapyHint(this Player player, string message, float duration = 3f, string tag = "default", int fontSize = 24)
    {
        if (player == null) return;

        var display = PlayerDisplay.Get(player);
        KillExpiry(player.Id, tag);
        player.ClearCapyHint(tag);

        var hint = new AbstractHint
        {
            Text = message,
            FontSize = fontSize,
            Tag = tag,
            Layer = HintLayer.Notification,
            Priority = 0
        };

        var dict = TaggedHints.GetOrAdd(player.Id, _ => new Dictionary<string, AbstractHint>());
        dict[tag] = hint;

        display.AddHint(hint);

        if (duration > 0f && duration < 3600f)
        {
            CoroutineHandle handle = Timing.CallDelayed(duration, () =>
            {
                RemoveTaggedHint(player.Id, tag, hint);
                display.RemoveHint(hint);
            });

            StoreExpiry(player.Id, tag, handle);
        }
    }

    public static void ClearCapyHint(this Player player, string tag)
    {
        if (player == null || string.IsNullOrEmpty(tag)) return;

        if (TaggedHints.TryGetValue(player.Id, out var dict))
        {
            if (dict.TryGetValue(tag, out var hint) && PlayerDisplay.Get(player) is { } display)
                display.RemoveHint(hint);

            dict.Remove(tag);
        }

        KillExpiry(player.Id, tag);
    }

    public static void ClearAllCapyHints(this Player player)
    {
        if (player == null) return;

        if (TaggedHints.TryRemove(player.Id, out var dict))
            dict.Clear();

        if (ExpiryHandles.TryRemove(player.Id, out var handles))
        {
            foreach (var handle in handles.Values)
                Timing.KillCoroutines(handle);
        }

        PlayerDisplay.Get(player)?.ClearHint();
    }

    internal static void CleanupPlayer(int playerId)
    {
        if (TaggedHints.TryRemove(playerId, out var dict))
            dict.Clear();

        if (ExpiryHandles.TryRemove(playerId, out var handles))
        {
            foreach (var handle in handles.Values)
                Timing.KillCoroutines(handle);
        }
    }

    private static void RemoveTaggedHint(int playerId, string tag, AbstractHint hint)
    {
        if (TaggedHints.TryGetValue(playerId, out var dict) &&
            dict.TryGetValue(tag, out var current) && ReferenceEquals(current, hint))
        {
            dict.Remove(tag);
        }
    }

    private static void StoreExpiry(int playerId, string tag, CoroutineHandle handle)
    {
        var dict = ExpiryHandles.GetOrAdd(playerId, _ => new Dictionary<string, CoroutineHandle>());
        dict[tag] = handle;
    }

    private static void KillExpiry(int playerId, string tag)
    {
        if (!ExpiryHandles.TryGetValue(playerId, out var dict)) return;
        if (!dict.TryGetValue(tag, out var handle)) return;

        dict.Remove(tag);
        Timing.KillCoroutines(handle);
    }
}
