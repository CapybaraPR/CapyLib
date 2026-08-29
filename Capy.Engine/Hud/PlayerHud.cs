using System;
using System.Collections.Generic;
using Capy.Engine.Hud.Panels;
using Exiled.API.Features;
using MEC;

namespace Capy.Engine.Hud;

public class PlayerHud
{
    public Player Player { get; }
    private readonly List<HudPanel> _panels;
    private CoroutineHandle _coroutine;
    private bool _destroyed;

    public PlayerHud(Player player)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        _panels = new List<HudPanel>
        {
            new AliveBrandPanel(player),
            new RoundTimePanel(player),
            new WarheadStatusPanel(player),
            new GeneratorStatusPanel(player),
            new RespawnTimerPanel(player),
            new SpectatorListPanel(player),
            new SpectatorBottomPanel(player),
            new ItemHudPanel(player)
        };

        _coroutine = Timing.RunCoroutine(UpdateLoop());
    }

    private IEnumerator<float> UpdateLoop()
    {
        yield return Timing.WaitForSeconds(1.0f);

        while (!_destroyed)
        {
            if (Player != null && Player.ReferenceHub != null && Player.ReferenceHub.connectionToClient != null && Player.ReferenceHub.connectionToClient.isReady && Player.Role.Type != PlayerRoles.RoleTypeId.None)
            {
                foreach (var panel in _panels)
                {
                    try { panel.Update(); }
                    catch (Exception e)
                    {
                        Log.Error($"[PlayerHud] Error in panel {panel.GetType().Name}: {e}");
                    }
                }
            }

            yield return Timing.WaitForSeconds(0.2f);
        }
    }

    public void Destroy()
    {
        _destroyed = true;
        Timing.KillCoroutines(_coroutine);
        foreach (var panel in _panels)
        {
            try { panel.Destroy(); }
            catch { }
        }
        _panels.Clear();
    }
}
