using System.Collections.Concurrent;
using Capy.API.Http;
using Capy.Core.API;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;

namespace Capy.API.Stats;

/// <summary>
/// Обработчики событий Exiled для сбора статистики и отправки логов на внешний API.
/// </summary>
public class StatsEventHandlers
{
    private readonly ConcurrentDictionary<int, PlayerSession> _activeSessions = new();
    private readonly ICapyLogger _log;
    private readonly StatsConfig _config;

    public StatsEventHandlers(ICapyLogger log, StatsConfig config)
    {
        _log = log;
        _config = config;
    }

    public void OnVerified(VerifiedEventArgs ev)
    {
        if (ev.Player == null) return;

        bool isAdmin = ev.Player.RemoteAdminAccess || !string.IsNullOrEmpty(ev.Player.GroupName);

        var session = new PlayerSession
        {
            UserId = ev.Player.UserId,
            Nickname = ev.Player.Nickname,
            IpAddress = ev.Player.IPAddress,
            IsAdmin = isAdmin,
            JoinTime = DateTime.UtcNow
        };

        _activeSessions[ev.Player.Id] = session;

        if (_config.EnableAuthLogging)
        {
            var authPayload = new
            {
                event_type = "JOIN",
                user_id = session.UserId,
                nickname = session.Nickname,
                ip = session.IpAddress,
                is_admin = session.IsAdmin,
                timestamp = DateTime.UtcNow.ToString("o")
            };

            _ = CapyApiClient.PostFireAndForgetAsync($"{_config.ApiUrl.TrimEnd('/')}/auth-log", authPayload);
        }
    }

    public void OnDied(DiedEventArgs ev)
    {
        if (ev.Player != null && _activeSessions.TryGetValue(ev.Player.Id, out var victimSession))
        {
            victimSession.Deaths++;
        }

        if (ev.Attacker != null && ev.Attacker != ev.Player && _activeSessions.TryGetValue(ev.Attacker.Id, out var attackerSession))
        {
            attackerSession.Kills++;
        }
    }

    public void OnLeft(LeftEventArgs ev)
    {
        if (ev.Player == null) return;

        if (_activeSessions.TryRemove(ev.Player.Id, out var session))
        {
            try
            {
                if (CapyPlugin.Instance?.Database != null && !string.IsNullOrWhiteSpace(session.UserId))
                {
                    var playerModel = CapyPlugin.Instance.Database.GetPlayer(session.UserId) ?? new Capy.Core.Database.Models.PlayerDataModel
                    {
                        Id = session.UserId,
                        FirstJoin = session.JoinTime
                    };

                    playerModel.LastNickname = !string.IsNullOrWhiteSpace(session.Nickname) ? session.Nickname : playerModel.LastNickname;
                    playerModel.LastIp = !string.IsNullOrWhiteSpace(session.IpAddress) ? session.IpAddress : playerModel.LastIp;
                    playerModel.Kills += session.Kills;
                    playerModel.Deaths += session.Deaths;
                    playerModel.RoundsPlayed += session.RoundsPlayed;
                    playerModel.TotalPlaytimeSeconds += session.SessionDurationSeconds;
                    playerModel.LastSeen = DateTime.UtcNow;

                    CapyPlugin.Instance.Database.SavePlayer(playerModel);
                }
            }
            catch (Exception dex)
            {
                _log?.Error($"[StatsTracker] Ошибка сохранения статистики игрока: {dex.Message}");
            }

            if (_config.EnableStatsTracking && !string.IsNullOrWhiteSpace(_config.ApiUrl))
            {
                var statsPayload = new
                {
                    user_id = session.UserId,
                    nickname = session.Nickname,
                    kills = session.Kills,
                    deaths = session.Deaths,
                    playtime_seconds = session.SessionDurationSeconds,
                    timestamp = DateTime.UtcNow.ToString("o")
                };

                _ = CapyApiClient.PostFireAndForgetAsync($"{_config.ApiUrl.TrimEnd('/')}/stats/update", statsPayload);
            }

            if (_config.EnableAuthLogging && !string.IsNullOrWhiteSpace(_config.ApiUrl))
            {
                var authPayload = new
                {
                    event_type = "LEAVE",
                    user_id = session.UserId,
                    nickname = session.Nickname,
                    duration = session.SessionDurationSeconds,
                    timestamp = DateTime.UtcNow.ToString("o")
                };

                _ = CapyApiClient.PostFireAndForgetAsync($"{_config.ApiUrl.TrimEnd('/')}/auth-log", authPayload);
            }
        }
    }

    public void OnRoundEnded(RoundEndedEventArgs ev)
    {
        foreach (var session in _activeSessions.Values)
        {
            session.RoundsPlayed++;
        }
    }

    public void Clear()
    {
        _activeSessions.Clear();
    }
}
