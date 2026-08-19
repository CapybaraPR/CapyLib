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
        sb.AppendLine("<color=#ffa94e>============================================================</color>");
        sb.AppendLine($"<b><color=#ffd285>           [ СТАТИСТИКА ИГРОКА: {targetNickname} ]</color></b>");
        sb.AppendLine("<color=#ffa94e>============================================================</color>");
        sb.AppendLine($"<color=#58b9ff>* SteamID64:</color> <color=#ffffff>{cleanId}</color>");
        sb.AppendLine($"<color=#58b9ff>* Общий онлайн:</color> <color=#ffd285>{FormatTime(playtimeSeconds)}</color>");
        sb.AppendLine($"<color=#58b9ff>* Сыграно раундов:</color> <color=#ffffff>{rounds}</color>");
        sb.AppendLine($"<color=#58b9ff>* Убийств:</color> <color=#a3e635>{kills}</color> <color=#c2c2c2>|</color> <color=#58b9ff>Смертей:</color> <color=#f87171>{deaths}</color> <color=#c2c2c2>|</color> <color=#58b9ff>K/D:</color> <color=#ffd285>{kd:F2}</color>");
        sb.AppendLine($"<color=#58b9ff>* Побед:</color> <color=#a3e635>{wins}</color>");
        sb.AppendLine($"<color=#58b9ff>* Первый вход:</color> <color=#c2c2c2>{firstJoin:dd.MM.yyyy}</color>");

        if (staff != null && staff.IsActive)
        {
            int totalPunishments = staff.BansCount + staff.MutesCount + staff.KicksCount;
            sb.AppendLine("<color=#ffa94e>------------------------------------------------------------</color>");
            sb.AppendLine("<color=#f87171>>> СЛУЖЕБНАЯ ИНФОРМАЦИЯ АДМИНИСТРАЦИИ:</color>");
            sb.AppendLine($"<color=#58b9ff>* Должность:</color> <color=#ffd285>{staff.Group} [{staff.ServerScope.ToUpperInvariant()}]</color>");
            sb.AppendLine($"<color=#58b9ff>* Онлайн за неделю:</color> <color=#ffd285>{FormatTime(staff.WeeklyPlaytimeSeconds)}</color> <color=#c2c2c2>(Норма: >=4ч)</color>");
            sb.AppendLine($"<color=#58b9ff>* Выдано наказаний:</color> <color=#f87171>{totalPunishments}</color> <color=#c2c2c2>(Банов: {staff.BansCount}, Мутов: {staff.MutesCount}, Киков: {staff.KicksCount})</color>");
            if (staff.DiscordUserId != 0)
                sb.AppendLine($"<color=#58b9ff>* Discord:</color> <color=#5865f2>{staff.DiscordUserId}</color>");
        }

        sb.Append("<color=#ffa94e>============================================================</color>");

        player.SendConsoleMessage(sb.ToString(), "white");
        response = string.Empty;
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
