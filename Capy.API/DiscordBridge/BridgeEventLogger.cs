using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Warhead;
using PlayerEvents = Exiled.Events.Handlers.Player;
using ServerEvents = Exiled.Events.Handlers.Server;
using WarheadEvents = Exiled.Events.Handlers.Warhead;

namespace Capy.API.DiscordBridge;

public static class BridgeCommandExecutionContext
{
    [ThreadStatic]
    public static bool IsActive;
}

public sealed class BridgeEventLogger
{
    private readonly DiscordBridgeConfig _config;
    private readonly BridgeEventStore _store;

    public BridgeEventLogger(DiscordBridgeConfig config, BridgeEventStore store)
    {
        _config = config;
        _store = store;
    }

    public void Subscribe()
    {
        PlayerEvents.Kicking += OnKicking;
        PlayerEvents.Banning += OnBanning;
        PlayerEvents.IssuingMute += OnIssuingMute;
        PlayerEvents.RevokingMute += OnRevokingMute;
        PlayerEvents.Verified += OnVerified;
        PlayerEvents.Left += OnLeft;
        PlayerEvents.Died += OnDied;
        PlayerEvents.SentValidCommand += OnSentValidCommand;
        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
        ServerEvents.RoundStarted += OnRoundStarted;
        ServerEvents.RoundEnded += OnRoundEnded;
        ServerEvents.RestartingRound += OnRestartingRound;
        ServerEvents.RespawnedTeam += OnRespawnedTeam;
        ServerEvents.ReportingCheater += OnReportingCheater;
        ServerEvents.LocalReporting += OnLocalReporting;
        ServerEvents.Unbanning += OnUnbanning;
        ServerEvents.Unbanned += OnUnbanned;
        ServerEvents.ReloadedConfigs += OnReloadedConfigs;
        ServerEvents.ReloadedPlugins += OnReloadedPlugins;
        WarheadEvents.Starting += OnWarheadStarting;
        WarheadEvents.Stopping += OnWarheadStopping;
        WarheadEvents.Detonated += OnWarheadDetonated;
    }

    public void Unsubscribe()
    {
        PlayerEvents.Kicking -= OnKicking;
        PlayerEvents.Banning -= OnBanning;
        PlayerEvents.IssuingMute -= OnIssuingMute;
        PlayerEvents.RevokingMute -= OnRevokingMute;
        PlayerEvents.Verified -= OnVerified;
        PlayerEvents.Left -= OnLeft;
        PlayerEvents.Died -= OnDied;
        PlayerEvents.SentValidCommand -= OnSentValidCommand;
        ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
        ServerEvents.RoundStarted -= OnRoundStarted;
        ServerEvents.RoundEnded -= OnRoundEnded;
        ServerEvents.RestartingRound -= OnRestartingRound;
        ServerEvents.RespawnedTeam -= OnRespawnedTeam;
        ServerEvents.ReportingCheater -= OnReportingCheater;
        ServerEvents.LocalReporting -= OnLocalReporting;
        ServerEvents.Unbanning -= OnUnbanning;
        ServerEvents.Unbanned -= OnUnbanned;
        ServerEvents.ReloadedConfigs -= OnReloadedConfigs;
        ServerEvents.ReloadedPlugins -= OnReloadedPlugins;
        WarheadEvents.Starting -= OnWarheadStarting;
        WarheadEvents.Stopping -= OnWarheadStopping;
        WarheadEvents.Detonated -= OnWarheadDetonated;
    }

    public void LogDiscordCommand(string command, string output, bool success, string actor, string access)
    {
        if (!_config.LogCommands)
            return;

        List<BridgeLogField> fields = CommandLogFormatter.BuildFields(command, output, success, "Discord", $"{actor} ({access})", _config);
        _store.Append(
            BridgeLogCategory.Commands,
            success ? "discord_command_succeeded" : "discord_command_failed",
            CommandLogFormatter.BuildTitle(command, success),
            success ? "Команда выполнена через Discord." : "Команда отклонена или завершилась ошибкой.",
            success ? "info" : "warning",
            fields);
    }

    public void LogDiscordDeniedCommand(string command, string reason, string actor, string access)
    {
        LogDiscordCommand(command, reason, false, actor, access);
    }

    private void OnKicking(KickingEventArgs ev) => AddPunishment(
        "kick_attempt", "Кик игрока", ev.IsAllowed ? "info" : "warning",
        ev.IsAllowed ? "Попытка кика разрешена." : "Попытка кика отклонена.",
        Field("Исполнитель", DescribePlayer(ev.Player)), Field("Цель", DescribePlayer(ev.Target)), Field("Причина", ev.Reason));

    private void OnBanning(BanningEventArgs ev) => AddPunishment(
        "ban_attempt", "Бан игрока", ev.IsAllowed ? "info" : "warning",
        ev.IsAllowed ? "Попытка бана разрешена." : "Попытка бана отклонена.",
        Field("Исполнитель", DescribePlayer(ev.Player)), Field("Цель", DescribePlayer(ev.Target)),
        Field("Срок, сек.", ev.Duration.ToString(CultureInfo.InvariantCulture)), Field("Причина", ev.Reason),
        Field("Тип", ev.CommandSender?.GetType().Name ?? "unknown"));

    private void OnIssuingMute(IssuingMuteEventArgs ev) => AddPunishment(
        "mute", "Мут игрока", ev.IsAllowed ? "info" : "warning",
        ev.IsAllowed ? "Игроку выдан мут." : "Выдача мута отклонена.",
        Field("Игрок", DescribePlayer(ev.Player)), Field("Тип", ev.IsIntercom ? "Интерком" : "Голосовой чат"));

    private void OnRevokingMute(RevokingMuteEventArgs ev) => AddPunishment(
        "unmute", "Снятие мута", ev.IsAllowed ? "info" : "warning",
        ev.IsAllowed ? "С игрока снят мут." : "Снятие мута отклонено.",
        Field("Игрок", DescribePlayer(ev.Player)), Field("Тип", ev.IsIntercom ? "Интерком" : "Голосовой чат"));

    private void OnUnbanning(UnbanningEventArgs ev) => AddPunishment(
        "unban_attempt", "Разбан игрока", ev.IsAllowed ? "info" : "warning",
        ev.IsAllowed ? "Попытка разбана разрешена." : "Попытка разбана отклонена.",
        Field("Идентификатор", ev.TargetId), Field("Тип", ev.BanType.ToString()));

    private void OnUnbanned(UnbannedEventArgs ev) => AddPunishment(
        "unbanned", "Игрок разбанен", "info", "Запись о бане удалена.",
        Field("Идентификатор", ev.TargetId), Field("Тип", ev.BanType.ToString()));

    private void OnVerified(VerifiedEventArgs ev) => AddServer(
        "player_verified", "Игрок подключился", $"{DescribePlayer(ev.Player)} прошёл проверку.",
        Field("Онлайн", ConnectedCount().ToString(CultureInfo.InvariantCulture)));

    private void OnLeft(LeftEventArgs ev) => AddServer(
        "player_left", "Игрок отключился", $"{DescribePlayer(ev.Player)} покинул сервер.",
        Field("Онлайн", ConnectedCount().ToString(CultureInfo.InvariantCulture)));

    private void OnDied(DiedEventArgs ev) => AddServer(
        "player_died", "Игрок погиб", $"{DescribePlayer(ev.Player)} погиб.",
        Field("Убийца", ev.Attacker == null ? "Нет атакующего" : DescribePlayer(ev.Attacker)),
        Field("Причина", ev.DamageHandler?.Type.ToString() ?? "Unknown"));

    private void OnSentValidCommand(SentValidCommandEventArgs ev)
    {
        string command = ev.Query ?? ev.Command?.Command ?? string.Empty;
        if (BridgeCommandExecutionContext.IsActive || !_config.LogCommands ||
            command.TrimStart().StartsWith("$", StringComparison.Ordinal) || IsTrustedAdmin(ev.Player))
            return;

        List<BridgeLogField> fields = CommandLogFormatter.BuildFields(
            command, ev.Response ?? string.Empty, ev.Result, ev.Type.ToString(), DescribePlayer(ev.Player), _config);
        _store.Append(
            BridgeLogCategory.Commands,
            ev.Result ? "server_command_succeeded" : "server_command_failed",
            CommandLogFormatter.BuildTitle(command, ev.Result),
            ev.Result ? "Команда выполнена в SCP:SL." : "Команда завершилась ошибкой.",
            ev.Result ? "info" : "warning",
            fields);
    }

    private void OnWaitingForPlayers() => AddRound("waiting_for_players", "Сервер ожидает игроков", "Сервер перешёл в состояние ожидания.");

    private void OnRoundStarted() => AddRound(
        "round_started",
        "Раунд начался",
        "Игровой раунд запущен.",
        "info",
        Field("Игроков", ConnectedCount().ToString(CultureInfo.InvariantCulture)));

    private void OnRoundEnded(RoundEndedEventArgs ev) => AddRound(
        "round_ended", "Раунд завершён", "Игровой раунд завершён.", "info",
        Field("Победившая сторона", ev.LeadingTeam.ToString()), Field("До рестарта, сек.", ev.TimeToRestart.ToString(CultureInfo.InvariantCulture)));

    private void OnRestartingRound() => AddRound("round_restarting", "Рестарт раунда", "Сервер перезапускает раунд.");

    private void OnRespawnedTeam(RespawnedTeamEventArgs ev) => AddRound(
        "team_respawned", "Волна возрождения", "На сервере появилась новая командная волна.", "info",
        Field("Команда", ev.Wave?.ToString() ?? "Unknown"), Field("Игроков", ev.Players.Count().ToString(CultureInfo.InvariantCulture)));

    private void OnWarheadStarting(StartingEventArgs ev) => AddRound(
        "warhead_starting", "Запуск боеголовки", ev.IsAllowed ? "Запуск боеголовки разрешён." : "Запуск боеголовки отклонён.",
        ev.IsAllowed ? "warning" : "info", Field("Инициатор", DescribePlayer(ev.Player)), Field("Автоматически", ev.IsAuto.ToString()));

    private void OnWarheadStopping(StoppingEventArgs ev) => AddRound(
        "warhead_stopping", "Остановка боеголовки", ev.IsAllowed ? "Остановка боеголовки разрешена." : "Остановка боеголовки отклонена.",
        ev.IsAllowed ? "warning" : "info", Field("Инициатор", DescribePlayer(ev.Player)));

    private void OnWarheadDetonated() => AddRound("warhead_detonated", "Боеголовка взорвалась", "Боеголовка была приведена в действие.", "danger");

    private void OnReportingCheater(ReportingCheaterEventArgs ev) => AddReport(
        "cheater_report", "Жалоба на читера", ev.IsAllowed ? "Жалоба отправлена." : "Жалоба отклонена.", ev.IsAllowed ? "info" : "warning",
        Field("Жалоба", DescribePlayer(ev.Player)), Field("Цель", DescribePlayer(ev.Target)), Field("Причина", ev.Reason));

    private void OnLocalReporting(LocalReportingEventArgs ev) => AddReport(
        "local_report", "Жалоба администрации", ev.IsAllowed ? "Локальная жалоба отправлена." : "Локальная жалоба отклонена.", ev.IsAllowed ? "info" : "warning",
        Field("Жалоба", DescribePlayer(ev.Player)), Field("Цель", DescribePlayer(ev.Target)), Field("Причина", ev.Reason));

    private void OnReloadedConfigs() => AddServer("configs_reloaded", "Конфигурации перезагружены", "Конфигурации EXILED были перезагружены.");

    private void OnReloadedPlugins() => AddServer("plugins_reloaded", "Плагины перезагружены", "Список EXILED-плагинов был перезагружен.");

    private void AddPunishment(string type, string title, string severity, string description, params BridgeLogField[] fields) =>
        AppendIf(_config.LogPunishments, BridgeLogCategory.Punishments, type, title, description, severity, fields);

    private void AddRound(string type, string title, string description, string severity = "info", params BridgeLogField[] fields) =>
        AppendIf(_config.LogRounds, BridgeLogCategory.Rounds, type, title, description, severity, fields);

    private void AddServer(string type, string title, string description, params BridgeLogField[] fields) =>
        AppendIf(_config.LogServerEvents, BridgeLogCategory.Server, type, title, description, "info", fields);

    private void AddReport(string type, string title, string description, string severity, params BridgeLogField[] fields) =>
        AppendIf(_config.LogReports, BridgeLogCategory.Reports, type, title, description, severity, fields);

    private void AppendIf(
        bool enabled,
        BridgeLogCategory category,
        string type,
        string title,
        string description,
        string severity,
        IEnumerable<BridgeLogField> fields)
    {
        if (enabled)
            _store.Append(category, type, title, description, severity, fields.ToList());
    }

    private static BridgeLogField Field(string name, string value) => new() { Name = name, Value = value ?? string.Empty };

    private string DescribePlayer(Player? player)
    {
        if (player == null)
            return "Неизвестно";

        string result = $"{player.Nickname} (#{player.Id})";
        bool redactTrackingData = _config.RespectDoNotTrack && player.DoNotTrack;
        if (_config.IncludeLogUserIds && !redactTrackingData)
            result += $" [{player.UserId}]";
        if (_config.IncludeLogIpAddresses && !redactTrackingData)
            result += $" {player.IPAddress}";
        if (redactTrackingData)
            result += " [DNT]";
        return result;
    }

    private bool IsTrustedAdmin(Player? player) => player != null &&
        !string.IsNullOrWhiteSpace(player.UserId) &&
        (_config.TrustedAdminUserIds ?? new List<string>())
            .Any(id => id.Equals(player.UserId, StringComparison.OrdinalIgnoreCase));

    private static int ConnectedCount() => Player.List.Count(player => player != null && player.IsConnected && !player.IsHost);
}
