using System;
using Exiled.API.Features;
using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Models;
using Capy.Engine.Hints.Extensions;
using UnityEngine;
using Hint = Capy.Engine.Hints.Models.Hint;

namespace Capy.Engine.DevTools.Extensions;

public static class HintExtensions
{
    public static Hint ShowHint(this Player player, string message, float duration = 3f)
    {
        return Capy.Engine.Hints.Extensions.HintExtensions.ShowHint(player, message, duration);
    }

    public static Hint ShowHint(this Player player, string message, Vector2 position, float duration = 3f, HintVerticalAlign verticalAlign = HintVerticalAlign.Middle, HintAlignment alignment = HintAlignment.Center, int fontSize = 24, string tag = "default")
    {
        return Capy.Engine.Hints.Extensions.HintExtensions.ShowHint(player, message, position, duration, verticalAlign, alignment, fontSize, tag);
    }

    public static Hint ShowItemHud(this Player player, string message, Vector2 position, float time = 1.2f, string tag = "custom_item_hud")
    {
        return Capy.Engine.Hints.Extensions.HintExtensions.ShowItemHud(player, message, position, time, tag);
    }

    public static void ClearHints(this Player player)
    {
        Capy.Engine.Hints.Extensions.HintExtensions.ClearHints(player);
    }

    public static void ClearHints(this Player player, string tag)
    {
        Capy.Engine.Hints.Extensions.HintExtensions.ClearHints(player, tag);
    }
}
