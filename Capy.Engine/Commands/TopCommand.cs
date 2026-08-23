using System;
using System.Collections.Generic;
using System.Text;
using Capy.Core.Database.Models;
using CommandSystem;
using Exiled.API.Features;

namespace Capy.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class TopCommand : ICommand
{
    public string Command { get; } = "top";
    public string[] Aliases { get; } = { "leaderboard", "lb" };
    public string Description { get; } = "Таблица лидеров сервера по убийствам, времени или раундам.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        string mode = (arguments.Count > 0 && arguments.Array != null)
            ? arguments.Array[arguments.Offset].ToLowerInvariant()
            : "kills";

        string field = mode switch
        {
            "time" or "playtime" => nameof(PlayerDataModel.TotalPlaytimeSeconds),
            "rounds" => nameof(PlayerDataModel.RoundsPlayed),
            _ => nameof(PlayerDataModel.Kills)
        };

        IReadOnlyList<PlayerDataModel> top = CapyPlugin.Instance?.Database?.GetTopPlayers(field, 10)
                                             ?? Array.Empty<PlayerDataModel>();

        if (top.Count == 0)
        {
            response = "\n<color=#ffa94e>Таблица лидеров пока пуста. Сыграйте раунд, чтобы статистика сохранилась!</color>\n";
            return true;
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("<color=#ffa94e>============================================================</color>");

        if (mode == "time" || mode == "playtime")
        {
            sb.AppendLine("<b><color=#ffd285>              [ ТОП-10 ИГРОКОВ ПО ОНЛАЙНУ ]</color></b>");
            sb.AppendLine("<color=#ffa94e>============================================================</color>");

            int rank = 1;
            foreach (var p in top)
            {
                string rankTag = rank <= 3 ? $"<color=#ffd285>[#{rank}]</color>" : $"<color=#c2c2c2>[#{rank}]</color>";
                string name = !string.IsNullOrEmpty(p.LastNickname) ? p.LastNickname : p.Id.Replace("@steam", "");
                sb.AppendLine($"{rankTag} <color=#ffffff>{name}</color> <color=#c2c2c2>--</color> <color=#ffd285>{FormatTime(p.TotalPlaytimeSeconds)}</color> <color=#c2c2c2>(Раундов:</color> <color=#58b9ff>{p.RoundsPlayed}</color><color=#c2c2c2>)</color>");
                rank++;
            }
        }
        else if (mode == "rounds")
        {
            sb.AppendLine("<b><color=#ffd285>             [ ТОП-10 ИГРОКОВ ПО РАУНДАМ ]</color></b>");
            sb.AppendLine("<color=#ffa94e>============================================================</color>");

            int rank = 1;
            foreach (var p in top)
            {
                string rankTag = rank <= 3 ? $"<color=#ffd285>[#{rank}]</color>" : $"<color=#c2c2c2>[#{rank}]</color>";
                string name = !string.IsNullOrEmpty(p.LastNickname) ? p.LastNickname : p.Id.Replace("@steam", "");
                sb.AppendLine($"{rankTag} <color=#ffffff>{name}</color> <color=#c2c2c2>--</color> <color=#58b9ff>{p.RoundsPlayed} раундов</color> <color=#c2c2c2>(Побед:</color> <color=#a3e635>{p.Wins}</color><color=#c2c2c2>)</color>");
                rank++;
            }
        }
        else
        {
            sb.AppendLine("<b><color=#ffd285>             [ ТОП-10 ИГРОКОВ ПО УБИЙСТВАМ ]</color></b>");
            sb.AppendLine("<color=#ffa94e>============================================================</color>");

            int rank = 1;
            foreach (var p in top)
            {
                string rankTag = rank <= 3 ? $"<color=#ffd285>[#{rank}]</color>" : $"<color=#c2c2c2>[#{rank}]</color>";
                string name = !string.IsNullOrEmpty(p.LastNickname) ? p.LastNickname : p.Id.Replace("@steam", "");
                double kd = p.Deaths > 0 ? (double)p.Kills / p.Deaths : p.Kills;
                sb.AppendLine($"{rankTag} <color=#ffffff>{name}</color> <color=#c2c2c2>--</color> <color=#a3e635>{p.Kills} убийств</color> <color=#c2c2c2>(K/D:</color> <color=#ffd285>{kd:F2}</color> <color=#c2c2c2>|</color> <color=#58b9ff>{FormatTime(p.TotalPlaytimeSeconds)}</color><color=#c2c2c2>)</color>");
                rank++;
            }
            sb.AppendLine("<color=#ffa94e>------------------------------------------------------------</color>");
            sb.AppendLine("<color=#ffd285>>> Совет: используйте '.top time' для просмотра топа по онлайну!</color>");
        }

        sb.Append("<color=#ffa94e>============================================================</color>");

        Player? player = Player.Get(sender);
        if (player != null)
        {
            player.SendConsoleMessage(sb.ToString(), "white");
            response = string.Empty;
            return true;
        }

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
