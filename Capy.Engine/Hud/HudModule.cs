using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.Events.EventArgs.Player;
using PlayerHandlers = Exiled.Events.Handlers.Player;
using Exiled.API.Features;

namespace Capy.Engine.Hud;

public static class HudModule
{
    private static readonly Dictionary<Player, PlayerHud> Huds = new();
    private static readonly Dictionary<Player, Player> SpectatedTargets = new();
    private static bool _isEnabled;

    public static void Enable()
    {
        if (_isEnabled) return;
        _isEnabled = true;

        PlayerHandlers.Verified += OnPlayerVerified;
        PlayerHandlers.Left += OnPlayerLeft;
        PlayerHandlers.ChangingSpectatedPlayer += OnChangingSpectatedPlayer;
        PlayerHandlers.Died += OnPlayerDied;

        foreach (var player in Player.List)
        {
            if (player != null && !player.IsHost)
                Huds[player] = new PlayerHud(player);
        }
    }

    public static void Disable()
    {
        if (!_isEnabled) return;
        _isEnabled = false;

        PlayerHandlers.Verified -= OnPlayerVerified;
        PlayerHandlers.Left -= OnPlayerLeft;
        PlayerHandlers.ChangingSpectatedPlayer -= OnChangingSpectatedPlayer;
        PlayerHandlers.Died -= OnPlayerDied;

        foreach (var hud in Huds.Values)
        {
            try { hud.Destroy(); }
            catch { }
        }
        Huds.Clear();
        SpectatedTargets.Clear();
    }

    public static Player? GetSpectatedTarget(Player spectator)
    {
        if (spectator == null) return null;
        if (SpectatedTargets.TryGetValue(spectator, out var target) && target != null && target.IsAlive)
        {
            return target;
        }

        return Player.List.FirstOrDefault(p => p != null && p.IsAlive && p.CurrentSpectatingPlayers != null && p.CurrentSpectatingPlayers.Contains(spectator));
    }

    private static void OnPlayerVerified(VerifiedEventArgs ev)
    {
        if (ev.Player != null && !ev.Player.IsHost && !Huds.ContainsKey(ev.Player))
        {
            Huds[ev.Player] = new PlayerHud(ev.Player);
        }
    }

    private static void OnPlayerLeft(LeftEventArgs ev)
    {
        if (ev.Player != null)
        {
            if (Huds.TryGetValue(ev.Player, out var hud))
            {
                hud.Destroy();
                Huds.Remove(ev.Player);
            }
            SpectatedTargets.Remove(ev.Player);
        }
    }

    private static void OnChangingSpectatedPlayer(ChangingSpectatedPlayerEventArgs ev)
    {
        if (ev.Player != null)
        {
            if (ev.NewTarget != null && ev.NewTarget.IsAlive)
            {
                SpectatedTargets[ev.Player] = ev.NewTarget;
            }
            else
            {
                SpectatedTargets.Remove(ev.Player);
            }
        }
    }

    private static void OnPlayerDied(DiedEventArgs ev)
    {
        if (ev.Player != null)
        {
            var spectators = SpectatedTargets.Where(kvp => kvp.Value == ev.Player).Select(kvp => kvp.Key).ToList();
            foreach (var spectator in spectators)
            {
                SpectatedTargets.Remove(spectator);
            }
        }
    }
}
