using Capy.Core.Features;
using Capy.Engine.Hints.Extensions;
using Capy.Engine.Hints.Utilities;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;

namespace Capy.Engine;

public static class PlayerStateCleaner
{
    private static bool _subscribed;

    public static void EnsureSubscribed()
    {
        if (_subscribed) return;

        Exiled.Events.Handlers.Player.Left += OnLeft;
        Exiled.Events.Handlers.Server.RestartingRound += OnRestartingRound;
        _subscribed = true;
    }

    public static void Shutdown()
    {
        if (!_subscribed) return;

        Exiled.Events.Handlers.Player.Left -= OnLeft;
        Exiled.Events.Handlers.Server.RestartingRound -= OnRestartingRound;
        _subscribed = false;
    }

    private static void OnLeft(LeftEventArgs ev)
    {
        Cleanup(ev.Player);
    }

    private static void OnRestartingRound()
    {
        foreach (var player in Player.List.ToList())
        {
            Cleanup(player);
        }
    }

    private static void Cleanup(Player? player)
    {
        if (player == null || string.IsNullOrEmpty(player.UserId)) return;

        try
        {
            ShowHintExtensions.CleanupPlayer(player);
            PlayerDisplay.RemovePlayer(player);
            GlobalCooldown.RemoveAllOwnedBy(player);
            GlobalCooldown.RemoveAllOwnedBy(player.UserId);
            PlayerData.Remove(player.UserId);
        }
        catch (Exception ex)
        {
            Log.Debug($"[PlayerStateCleaner] Ошибка очистки состояния {player.UserId}: {ex.Message}");
        }
    }
}
