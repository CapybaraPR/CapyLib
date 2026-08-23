using System;
using Capy.Engine.Hints;
using Capy.Engine.Hints.Service;
using Exiled.API.Features;

namespace Capy.Engine.Hud;

/// <summary>
/// Базовый класс информационной панели интерфейса HUD.
/// </summary>
public abstract class HudPanel
{
    public Player Player { get; }
    protected AbstractHint? Hint { get; set; }
    protected string LastText { get; set; } = string.Empty;

    protected HudPanel(Player player)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
    }

    protected bool IsConnected => Player != null && Player.IsConnected;

    public abstract void Update();

    protected void SetText(string text, HintZone zone, int fontSize = 20, string tag = "hud_panel")
    {
        if (!IsConnected) return;

        var display = PlayerDisplay.Get(Player);

        if (string.IsNullOrWhiteSpace(text))
        {
            if (Hint != null)
            {
                display.RemoveHint(Hint);
                Hint = null;
                LastText = string.Empty;
            }
            return;
        }

        if (text == LastText && Hint != null)
            return;

        LastText = text;

        if (Hint == null)
        {
            Hint = new AbstractHint
            {
                Text = text,
                FontSize = fontSize,
                Tag = tag,
                Zone = zone,
                Layer = HintLayer.Notification,
                Priority = 0
            };
            display.AddHint(Hint);
        }
        else
        {
            Hint.Text = text;
            Hint.FontSize = fontSize;
            Hint.Zone = zone;
            display.Render();
        }
    }

    public virtual void Destroy()
    {
        if (Hint != null && IsConnected)
        {
            PlayerDisplay.Get(Player).RemoveHint(Hint);
            Hint = null;
        }
        LastText = string.Empty;
    }
}
