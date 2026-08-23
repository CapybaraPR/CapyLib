using System;
using System.Collections.Generic;
using Capy.Engine.Hud.Panels;
using Exiled.API.Features;
using MEC;

namespace Capy.Engine.Hud;

/// <summary>
/// Экземпляр контроллера HUD для отдельного игрока.
/// </summary>
public sealed class PlayerHud
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
            new RoundTimePanel(player),
            new SpectatorListPanel(player),
            new SpectatorBottomPanel(player),
            new WarheadStatusPanel(player),
            new GeneratorStatusPanel(player),
            new RespawnTimersPanel(player)
        };

        _coroutine = Timing.RunCoroutine(UpdateLoop());
    }

    private IEnumerator<float> UpdateLoop()
    {
        yield return Timing.WaitForSeconds(1.0f);

        while (!_destroyed)
        {
            if (Player != null && Player.IsConnected)
            {
                foreach (var panel in _panels)
                {
                    try
                    {
                        panel.Update();
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"[PlayerHud] Ошибка обновления панели {panel.GetType().Name}: {ex.Message}");
                    }
                }
            }

            yield return Timing.WaitForSeconds(0.5f);
        }
    }

    public void Destroy()
    {
        _destroyed = true;
        Timing.KillCoroutines(_coroutine);

        foreach (var panel in _panels)
        {
            try { panel.Destroy(); } catch { }
        }
        _panels.Clear();
    }
}
