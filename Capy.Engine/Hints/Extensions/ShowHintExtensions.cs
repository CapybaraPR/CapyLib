using Capy.Engine.Hints.Service;
using UnityEngine;

namespace Capy.Engine.Hints.Extensions;

/// <summary>
/// Удобные методы расширения для отображения подсказок и HUD на экране игрока.
/// </summary>
public static class ShowHintExtensions
{
    private static readonly Dictionary<Player, Dictionary<string, AbstractHint>> TaggedHints = new();

    public static void ShowCapyHint(this Player player, string message, float duration = 3f, string tag = "default", int fontSize = 24)
    {
        if (player == null) return;

        var display = PlayerDisplay.Get(player);
        player.ClearCapyHint(tag);

        var hint = new AbstractHint
        {
            Text = message,
            FontSize = fontSize,
            Tag = tag,
            Layer = HintLayer.Notification,
            Priority = 0
        };

        if (!TaggedHints.TryGetValue(player, out var dict))
        {
            dict = new Dictionary<string, AbstractHint>();
            TaggedHints[player] = dict;
        }
        dict[tag] = hint;

        display.AddHint(hint);

        if (duration > 0f && duration < 3600f)
        {
            Task.Run(async () =>
            {
                await Task.Delay((int)(duration * 1000));
                display.RemoveHint(hint);
                if (TaggedHints.TryGetValue(player, out var playerDict))
                {
                    playerDict.Remove(tag);
                }
            });
        }
    }

    public static void ClearCapyHint(this Player player, string tag)
    {
        if (player == null || string.IsNullOrEmpty(tag)) return;

        if (TaggedHints.TryGetValue(player, out var dict) && dict.TryGetValue(tag, out var hint))
        {
            PlayerDisplay.Get(player)?.RemoveHint(hint);
            dict.Remove(tag);
        }
    }

    public static void ClearAllCapyHints(this Player player)
    {
        if (player == null) return;

        PlayerDisplay.Get(player)?.ClearHint();
        TaggedHints.Remove(player);
    }
}
