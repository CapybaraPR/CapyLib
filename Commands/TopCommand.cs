using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Capy.Core.Database.Models;
using CommandSystem;
using Exiled.API.Features;

namespace Capy.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class TopCommand : ICommand
{
    public string Command { get; } = "top";
    public string[] Aliases { get; } = { "топ", "лидеры", "leaderboard", "lb" };
    public string Description { get; } = "Таблица лидеров сервера по убийствам, времени или раундам.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        string mode = (arguments.Count > 0 && arguments.Array != null)
            ? arguments.Array[arguments.Offset].ToLowerInvariant()
            : "kills";

        IEnumerable<PlayerDataModel> all = CapyPlugin.Instance?.Database?.GetAllPlayers() ?? Enumerable.Empty<PlayerDataModel>();
        List<PlayerDataModel> list = all.ToList();

        if (list.Count == 0)
        {
            response = "\nТаблица лидеров пока пуста. Сыграйте раунд, чтобы статистика сохранилась!\n";
            return true;
        }

        var sb = new StringBuilder();
        sb.AppendLine("============================================================");

        if (mode == "time" || mode == "время" || mode == "онлайн" || mode == "playtime")
        {
            sb.AppendLine("              [ ТОП-10 ИГРОКОВ ПО ОНЛАЙНУ ]");
            sb.AppendLine("============================================================");

            var sorted = list.OrderByDescending(p => p.TotalPlaytimeSeconds).Take(10).ToList();
            int rank = 1;
            foreach (var p in sorted)
            {
                string rankTag = $"[#{rank}]";
                string name = !string.IsNullOrEmpty(p.LastNickname) ? p.LastNickname : p.Id.Replace("@steam", "");
                sb.AppendLine($"{rankTag} {name} -- {FormatTime(p.TotalPlaytimeSeconds)} (Раундов: {p.RoundsPlayed})");
                rank++;
            }
        }
        else if (mode == "rounds" || mode == "раунды")
        {
            sb.AppendLine("             [ ТОП-10 ИГРОКОВ ПО РАУНДАМ ]");
            sb.AppendLine("============================================================");

            var sorted = list.OrderByDescending(p => p.RoundsPlayed).Take(10).ToList();
            int rank = 1;
            foreach (var p in sorted)
            {
                string rankTag = $"[#{rank}]";
                string name = !string.IsNullOrEmpty(p.LastNickname) ? p.LastNickname : p.Id.Replace("@steam", "");
                sb.AppendLine($"{rankTag} {name} -- {p.RoundsPlayed} раундов (Побед: {p.Wins})");
                rank++;
            }
        }
        else
        {
            sb.AppendLine("             [ ТОП-10 ИГРОКОВ ПО УБИЙСТВАМ ]");
            sb.AppendLine("============================================================");

            var sorted = list.OrderByDescending(p => p.Kills).Take(10).ToList();
            int rank = 1;
            foreach (var p in sorted)
            {
                string rankTag = $"[#{rank}]";
                string name = !string.IsNullOrEmpty(p.LastNickname) ? p.LastNickname : p.Id.Replace("@steam", "");
                double kd = p.Deaths > 0 ? (double)p.Kills / p.Deaths : p.Kills;
                sb.AppendLine($"{rankTag} {name} -- {p.Kills} убийств (K/D: {kd:F2} | {FormatTime(p.TotalPlaytimeSeconds)})");
                rank++;
            }
            sb.AppendLine("------------------------------------------------------------");
            sb.AppendLine(">> Совет: используйте '.top time' для просмотра топа по онлайну!");
        }

        sb.AppendLine("============================================================");

        string colored = HelpMessageBuilder.Colorize(sb.ToString());
        Player? player = Player.Get(sender);
        if (player != null)
        {
            player.SendConsoleMessage(colored, "white");
            response = string.Empty;
            return true;
        }

        response = colored;
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
