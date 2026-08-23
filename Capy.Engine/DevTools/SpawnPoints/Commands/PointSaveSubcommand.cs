using System;
using CommandSystem;
using Exiled.API.Features;

namespace Capy.Engine.DevTools.SpawnPoints.Commands;

public class PointSaveSubcommand : ICommand
{
    public string Command => "save";
    public string[] Aliases => new[] { "add" };
    public string Description => "Сохранить текущую точку под указанным именем в конфиг.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player == null || player.ReferenceHub == null)
        {
            response = "Вы должны быть игроком на сервере!";
            return false;
        }

        string name = arguments.Count >= 1 ? arguments.At(0) : $"SavedPoint_{DateTime.Now:HHmmss}";

        try
        {
            var data = SpawnPointTool.CaptureCurrentPosition(player, name);
            SpawnPointTool.SavePoint(data);

            response = $"Успешно сохранена точка '{name}' в комнате {data.RoomName}!\nC# Код:\n{data.CsSnippetRoom}";
            return true;
        }
        catch (Exception ex)
        {
            response = $"Ошибка сохранения точки: {ex.Message}";
            return false;
        }
    }
}
