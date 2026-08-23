using System;
using Capy.Engine.Hints;
using Exiled.API.Features;

namespace Capy.Engine.Hud.Panels;

/// <summary>
/// Панель статуса обратного отсчета Альфа-боеголовки.
/// </summary>
public sealed class WarheadStatusPanel : HudPanel
{
    public WarheadStatusPanel(Player player) : base(player) { }

    public override void Update()
    {
        if (!IsConnected || !Warhead.IsInProgress)
        {
            SetText(string.Empty, HintZone.TopCenter);
            return;
        }

        int secondsLeft = Math.Max(0, (int)Math.Ceiling(Warhead.DetonationTimer));
        TimeSpan ts = TimeSpan.FromSeconds(secondsLeft);
        string timeStr = $"{ts.Minutes:D2}:{ts.Seconds:D2}";

        string text = $"<color=#ff2222><b>🚨 АЛЬФА-БОЕГОЛОВКА: {timeStr} 🚨</b></color>";

        SetText(text, HintZone.TopCenter, fontSize: 22, tag: "hud_warhead");
    }
}
