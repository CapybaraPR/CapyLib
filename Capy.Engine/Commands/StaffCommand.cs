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
public sealed class StaffCommand : ICommand
{
    public string Command { get; } = "staff";
    public string[] Aliases { get; } = { "duty" };
    public string Description { get; } = "Проверить статус сотрудника, часы за неделю и норму.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player == null || !player.IsConnected || string.IsNullOrWhiteSpace(player.UserId))
        {
            response = "Команда доступна только игроку на сервере.";
            return false;
        }

        string targetUserId = player.UserId;
        string targetNickname = player.Nickname;

        if (arguments.Count > 0 && arguments.Array != null && (player.RemoteAdminAccess || !string.IsNullOrEmpty(player.GroupName)))
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
        }

        StaffMemberModel? staff = DiscordBridgeModule.StaffService?.GetStaff(targetUserId);
        if (staff == null || !staff.IsActive)
        {
            string error = "\n<color=#f87171>[Отказ в доступе] Данный аккаунт не числится в активном составе персонала сервера.</color>\n";
            player.SendConsoleMessage(error, "white");
            response = string.Empty;
            return false;
        }

        long weeklySec = staff.WeeklyPlaytimeSeconds;
        long quotaSec = 4 * 3600; // 4 hours
        double percent = (double)weeklySec / quotaSec * 100.0;
        string progressStatus = weeklySec >= quotaSec
            ? $"<color=#a3e635>[{percent:F0}% • НОРМА ВЫПОЛНЕНА]</color>"
            : $"<color=#ffd285>[{percent:F0}% • Осталось {FormatTime(quotaSec - weeklySec)}]</color>";

        int totalPunishments = staff.BansCount + staff.MutesCount + staff.KicksCount;
        string cleanId = targetUserId.Replace("@steam", "").Replace("@discord", "");

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("<color=#ffa94e>============================================================</color>");
        sb.AppendLine($"<b><color=#ffd285>          [ СЛУЖЕБНЫЙ ПРОФИЛЬ СОТРУДНИКА: {targetNickname} ]</color></b>");
        sb.AppendLine("<color=#ffa94e>============================================================</color>");
        sb.AppendLine($"<color=#58b9ff>* SteamID64:</color> <color=#ffffff>{cleanId}</color>");
        sb.AppendLine($"<color=#58b9ff>* Должность:</color> <color=#ffd285>{staff.Group}</color> <color=#58b9ff>[{staff.ServerScope.ToUpperInvariant()}]</color>");
        sb.AppendLine($"<color=#58b9ff>* Статус в реестре:</color> <color=#a3e635>АКТИВЕН</color>");
        if (staff.DiscordUserId != 0)
            sb.AppendLine($"<color=#58b9ff>* Привязанный Discord ID:</color> <color=#5865f2>{staff.DiscordUserId}</color>");
        sb.AppendLine("<color=#ffa94e>------------------------------------------------------------</color>");
        sb.AppendLine("<color=#f87171>>> ВЫПОЛНЕНИЕ СЛУЖЕБНОЙ НОРМЫ (НЕДЕЛЯ):</color>");
        sb.AppendLine($"<color=#58b9ff>* Наиграно:</color> <color=#ffd285>{FormatTime(weeklySec)}</color> / <color=#ffffff>4 ч. 00 мин.</color> {progressStatus}");
        sb.AppendLine($"<color=#58b9ff>* Наказаний выдано:</color> <color=#f87171>{totalPunishments}</color> <color=#c2c2c2>(Банов: {staff.BansCount} | Мутов: {staff.MutesCount} | Киков: {staff.KicksCount})</color>");
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
