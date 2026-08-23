using System;
using System.Collections.Concurrent;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using PlayerHandlers = Exiled.Events.Handlers.Player;

namespace Capy.Engine.Hud;

/// <summary>
/// Главный контроллер экранного HUD и наблюдателей ядра CapyLib.
/// </summary>
public static class HudModule
{
    private static readonly ConcurrentDictionary<int, PlayerHud> Huds = new();
    private static readonly ConcurrentDictionary<int, int> SpectatedTargetIds = new();
    private static bool _isEnabled;

    public static void Enable()
    {
        if (_isEnabled) return;
        _isEnabled = true;

        PlayerHandlers.Verified += OnPlayerVerified;
        PlayerHandlers.Left += OnPlayerLeft;
        PlayerHandlers.ChangingSpectatedPlayer += OnChangingSpectatedPlayer;

        foreach (var player in Player.List)
        {
            if (player != null && player.IsConnected && !player.IsNPC)
                Huds[player.Id] = new PlayerHud(player);
        }

        Log.Info("[HudModule] Система экранного HUD и HUD наблюдателей активирована.");
    }

    public static void Disable()
    {
        if (!_isEnabled) return;
        _isEnabled = false;

        PlayerHandlers.Verified -= OnPlayerVerified;
        PlayerHandlers.Left -= OnPlayerLeft;
        PlayerHandlers.ChangingSpectatedPlayer -= OnChangingSpectatedPlayer;

        foreach (var hud in Huds.Values)
        {
            try { hud.Destroy(); } catch { }
        }
        Huds.Clear();
        SpectatedTargetIds.Clear();
    }

    public static Player? GetSpectatedTarget(Player spectator)
    {
        if (spectator == null) return null;

        if (SpectatedTargetIds.TryGetValue(spectator.Id, out int targetId))
        {
            var target = Player.Get(targetId);
            if (target != null && target.IsConnected && target.IsAlive)
                return target;
        }

        return Player.List.FirstOrDefault(p => p != null && p.IsConnected && p.IsAlive && p.CurrentSpectatingPlayers != null && p.CurrentSpectatingPlayers.Contains(spectator));
    }

    private static void OnPlayerVerified(VerifiedEventArgs ev)
    {
        if (ev.Player != null && !ev.Player.IsNPC)
            Huds[ev.Player.Id] = new PlayerHud(ev.Player);
    }

    private static void OnPlayerLeft(LeftEventArgs ev)
    {
        if (ev.Player == null) return;

        if (Huds.TryRemove(ev.Player.Id, out var hud))
            hud.Destroy();

        SpectatedTargetIds.TryRemove(ev.Player.Id, out _);
    }

    private static void OnChangingSpectatedPlayer(ChangingSpectatedPlayerEventArgs ev)
    {
        if (ev.Player == null) return;

        if (ev.NewTarget != null && ev.NewTarget.IsAlive)
            SpectatedTargetIds[ev.Player.Id] = ev.NewTarget.Id;
        else
            SpectatedTargetIds.TryRemove(ev.Player.Id, out _);
    }
}
