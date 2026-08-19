using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Capy.API.DiscordBridge;
using Capy.Core.Database.Models;
using CommandSystem;
using Exiled.API.Features;

namespace Capy.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class StatsCommand : ICommand
{
    public string Command { get; } = "stats";
    public string[] Aliases { get; } = { "стата", "статистика", "профиль", "profile", "me" };
    public string Description { get; } = "Просмотр личной статистики или профиля игрока.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player == null || !player.IsConnected || player.IsNPC ||
            !player.IsVerified || string.IsNullOrWhiteSpace(player.UserId))
        {
            response = "Команда доступна только авторизованному игроку на сервере.";
            return false;
        }

        string targetUserId = player.UserId;
        string targetNickname = player.Nickname;

        if (arguments.Count > 0 && arguments.Array != null)
        {
            string query = arguments.Array[arguments.Offset].Trim();
            Player? found = Player.List.FirstOrDefault(p =>
                p.Nickname.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                p.UserId.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                p.Id.ToString() == query);

            if (found != null)
            {
                targetUserId = found.UserId;
                targetNickname = found.Nickname;
            }
            else if (Regex.IsMatch(query, @"^\d{17}$"))
            {
                targetUserId = $"{query}@steam";
                targetNickname = query;
            }
            else if (query.EndsWith("@steam", StringComparison.OrdinalIgnoreCase))
            {
                targetUserId = query;
                targetNickname = query.Replace("@steam", "");
            }
        }

        PlayerDataModel? data = CapyPlugin.Instance?.Database?.GetPlayer(targetUserId);
        StaffMemberModel? staff = DiscordBridgeModule.StaffService?.GetStaff(targetUserId);

        int kills = data?.Kills ?? 0;
        int deaths = data?.Deaths ?? 0;
        int rounds = data?.RoundsPlayed ?? 0;
        int wins = data?.Wins ?? 0;
        long playtimeSeconds = data?.TotalPlaytimeSeconds ?? 0;
        DateTime firstJoin = data?.FirstJoin ?? DateTime.UtcNow;

        double kd = deaths > 0 ? (double)kills / deaths : kills;
        string cleanId = targetUserId.Replace("@steam", "").Replace("@discord", "");

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("============================================================");
        sb.AppendLine($"           [ СТАТИСТИКА ИГРОКА: {targetNickname} ]");
        sb.AppendLine("============================================================");
        sb.AppendLine($"* SteamID64: {cleanId}");
        sb.AppendLine($"* Общий онлайн: {FormatTime(playtimeSeconds)}");
        sb.AppendLine($"* Сыграно раундов: {rounds}");
        sb.AppendLine($"* Убийств: {kills} | Смертей: {deaths} | K/D: {kd:F2}");
        sb.AppendLine($"* Побед: {wins}");
        sb.AppendLine($"* Первый вход: {firstJoin:dd.MM.yyyy}");

        if (staff != null && staff.IsActive)
        {
            int totalPunishments = staff.BansCount + staff.MutesCount + staff.KicksCount;
            sb.AppendLine("------------------------------------------------------------");
            sb.AppendLine(">> СЛУЖЕБНАЯ ИНФОРМАЦИЯ АДМИНИСТРАЦИИ:");
            sb.AppendLine($"* Должность: {staff.Group} [{staff.ServerScope.ToUpperInvariant()}]");
            sb.AppendLine($"* Онлайн за неделю: {FormatTime(staff.WeeklyPlaytimeSeconds)} (Норма: >=4ч)");
            sb.AppendLine($"* Выдано наказаний: {totalPunishments} (Банов: {staff.BansCount}, Мутов: {staff.MutesCount}, Киков: {staff.KicksCount})");
            if (staff.DiscordUserId != 0)
                sb.AppendLine($"* Discord: {staff.DiscordUserId}");
        }

        sb.AppendLine("============================================================");
        response = sb.ToString();
        return true;
    }

    private static string FormatTime(long seconds)
    {
        if (seconds <= 0) return "0 мин.";
        TimeSpan ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours} ч. {ts.Minutes} мин.";
        return $"{ts.Minutes} мин.";
    }
}
