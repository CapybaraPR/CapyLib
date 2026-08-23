using System;
using CommandSystem;
using Exiled.API.Features;

namespace Capy.Engine.DevTools.SpawnPoints.Commands;

public class PointGetSubcommand : ICommand
{
    public string Command => "get";
    public string[] Aliases => new[] { "info", "pos", "here" };
    public string Description => "Снять текущие координаты игрока (комната, локальный смещение, C# код).";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player == null || player.ReferenceHub == null)
        {
            response = "Вы должны быть игроком на сервере!";
            return false;
        }

        try
        {
            var data = SpawnPointTool.CaptureCurrentPosition(player, "CurrentPos");

            response = $"📍 ИНФОРМАЦИЯ О ТОЧКЕ:\n" +
                       $"• Комната: {data.RoomName} ({data.Zone})\n" +
                       $"• Global Vector3: {data.CsSnippetGlobal}\n" +
                       $"• Local Offset: ({data.LocalOffset.x:F2}, {data.LocalOffset.y:F2}, {data.LocalOffset.z:F2})\n" +
                       $"• Local Rotation: ({data.LocalRotation.x:F2}, {data.LocalRotation.y:F2}, {data.LocalRotation.z:F2})\n\n" +
                       $"C# СНИППЕТ ДЛЯ КОДА:\n{data.CsSnippetRoom}";
            return true;
        }
        catch (Exception ex)
        {
            response = $"Ошибка получения координат: {ex.Message}";
            return false;
        }
    }
}
