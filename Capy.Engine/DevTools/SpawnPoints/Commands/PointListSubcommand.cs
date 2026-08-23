using System;
using System.Text;
using CommandSystem;

namespace Capy.Engine.DevTools.SpawnPoints.Commands;

public class PointListSubcommand : ICommand
{
    public string Command => "list";
    public string[] Aliases => new[] { "ls", "all" };
    public string Description => "Вывести список всех сохраненных спавнпоинтов.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var points = SpawnPointTool.GetSavedPoints();
        if (points.Count == 0)
        {
            response = "Список сохраненных точек пуст! Используйте /point save <имя> или Тулган.";
            return true;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"📋 СПИСОК СОХРАНЕННЫХ ТОЧЕК ({points.Count}):");
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            sb.AppendLine($"[{i + 1}] '{p.Name}' | Комната: {p.RoomName} ({p.Zone})");
            sb.AppendLine($"    • C# Код: {p.CsSnippetRoom}");
        }

        response = sb.ToString();
        return true;
    }
}
