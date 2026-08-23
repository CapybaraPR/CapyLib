using System;
using CommandSystem;
using Exiled.API.Features;

namespace Capy.Engine.DevTools.SpawnPoints.Commands;

public class PointRaySubcommand : ICommand
{
    public string Command => "ray";
    public string[] Aliases => new[] { "look", "target" };
    public string Description => "Снять координаты точки, куда направлен прицел игрока.";

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
            var data = SpawnPointTool.CaptureRaycastPosition(player, "RaycastTarget");

            response = $"📍 ИНФОРМАЦИЯ О ТОЧКЕ ПРИЦЕЛА (RAYCAST):\n" +
                       $"• Комната: {data.RoomName} ({data.Zone})\n" +
                       $"• Global Vector3: {data.CsSnippetGlobal}\n" +
                       $"• Local Offset: ({data.LocalOffset.x:F2}, {data.LocalOffset.y:F2}, {data.LocalOffset.z:F2})\n\n" +
                       $"C# СНИППЕТ ДЛЯ КОДА:\n{data.CsSnippetRoom}";
            return true;
        }
        catch (Exception ex)
        {
            response = $"Ошибка Raycast: {ex.Message}";
            return false;
        }
    }
}
