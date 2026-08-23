using System;
using System.Collections.Generic;
using Exiled.API.Features;
using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Models;
using Hint = Capy.Engine.Hints.Models.Hint;
using Capy.Engine.Hints.Utilities;
using MEC;
using UnityEngine;

namespace Capy.Engine.Hints.Extensions;

public static class HintExtensions
{
    private static readonly Dictionary<Player, Dictionary<string, Hint>> TaggedHints = new();

    public static Hint ShowHint(this Player player, string message, float duration = 3f)
    {
        // Default position Y=540 is middle of screen in 0..1080 canvas space
        return player.ShowHint(message, new Vector2(0, 540), duration, HintVerticalAlign.Middle, HintAlignment.Center, 24, "center");
    }

    public static Hint ShowHint(this Player player, string message, Vector2 position, float duration = 3f, HintVerticalAlign verticalAlign = HintVerticalAlign.Middle, HintAlignment alignment = HintAlignment.Center, int fontSize = 24, string tag = "default", HintLayer layer = HintLayer.Notification, int priority = 0)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));

        var display = PlayerDisplay.Get(player);
        player.ClearHints(tag);

        Hint hint = new Hint
        {
            Text = message,
            FontSize = fontSize,
            XCoordinate = position.x,
            YCoordinate = position.y,
            YCoordinateAlign = verticalAlign,
            Alignment = alignment,
            Tag = tag,
            Layer = layer,
            Priority = priority,
            SyncSpeed = HintSyncSpeed.Fast
        };

        if (!TaggedHints.TryGetValue(player, out var dict))
        {
            dict = new Dictionary<string, Hint>();
            TaggedHints[player] = dict;
        }
        dict[tag] = hint;

        display.AddHint(hint);

        if (duration > 0 && duration < 3600f)
        {
            Timing.CallDelayed(duration, () =>
            {
                try
                {
                    display.RemoveHint(hint);
                    if (TaggedHints.TryGetValue(player, out var playerDict) && playerDict.TryGetValue(tag, out var currentHint) && currentHint == hint)
                    {
                        playerDict.Remove(tag);
                    }
                }
                catch { }
            });
        }

        return hint;
    }

    public static Hint ShowItemHud(this Player player, string message, Vector2 position, float time = 1.2f, string tag = "custom_item_hud")
    {
        return player.ShowHint(message, position, time, HintVerticalAlign.Middle, HintAlignment.Left, 22, tag);
    }

    public static void ClearHints(this Player player)
    {
        if (player == null) return;
        PlayerDisplay.Get(player)?.ClearHint();
        if (TaggedHints.ContainsKey(player))
            TaggedHints.Remove(player);
    }

    public static void ClearHints(this Player player, string tag)
    {
        if (player == null || string.IsNullOrEmpty(tag)) return;

        if (TaggedHints.TryGetValue(player, out var dict) && dict.TryGetValue(tag, out var hint))
        {
            PlayerDisplay.Get(player)?.RemoveHint(hint);
            dict.Remove(tag);
        }
    }
}
