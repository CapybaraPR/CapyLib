using System;
using Capy.Engine.Hints.Models;
using Capy.Engine.Hints.Utilities;
using Exiled.API.Features;
using Hint = Capy.Engine.Hints.Models.Hint;

namespace Capy.Engine.Hud;

public abstract class HudPanel
{
    public Player Player { get; }
    protected Hint? Hint { get; set; }

    protected HudPanel(Player player)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
    }

    protected bool IsPlayerConnected => Player != null && Player.ReferenceHub != null && Player.ReferenceHub.connectionToClient != null && Player.ReferenceHub.connectionToClient.isReady && Player.Role.Type != PlayerRoles.RoleTypeId.None;

    protected bool EnsureHint()
    {
        if (Hint != null) return true;
        if (!IsPlayerConnected) return false;

        var display = PlayerDisplay.Get(Player);
        if (display == null) return false;

        try
        {
            display.SetMinUpdateInterval(TimeSpan.FromMilliseconds(50));
            CreateHint(display);
            display.ForceUpdate(true);
            return Hint != null;
        }
        catch
        {
            return false;
        }
    }

    protected abstract void CreateHint(PlayerDisplay display);

    public abstract void Update();

    protected void SetText(string text)
    {
        if (Hint != null && Hint.Text != text)
        {
            Hint.Text = text;
            if (IsPlayerConnected)
            {
                var display = PlayerDisplay.Get(Player);
                display?.ForceUpdate(true);
            }
        }
    }

    public virtual void Destroy()
    {
        if (Hint != null && Player != null)
        {
            try
            {
                var display = PlayerDisplay.Get(Player);
                if (display != null)
                {
                    display.RemoveHint(Hint);
                    display.ForceUpdate(true);
                }
            }
            catch { }
            Hint = null;
        }
    }
}
