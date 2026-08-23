using System;
using CommandSystem;

namespace Capy.Engine.DevTools.SpawnPoints.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class PointParentCommand : ParentCommand
{
    public PointParentCommand()
    {
        LoadGeneratedCommands();
    }

    public override string Command => "point";
    public override string[] Aliases => new[] { "spawntool", "coords", "whereami", "toolgun" };
    public override string Description => "Инструмент разработчика для снятия и сохранения координат спавнпоинтов.";

    public override void LoadGeneratedCommands()
    {
        RegisterCommand(new PointGetSubcommand());
        RegisterCommand(new PointRaySubcommand());
        RegisterCommand(new PointSaveSubcommand());
        RegisterCommand(new PointListSubcommand());
        RegisterCommand(new PointToolSubcommand());
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response = "Использование инструмента спавнпоинтов (/point):\n" +
                   "• point get — получить локальные и глобальные координаты стоящего места\n" +
                   "• point ray — получить координаты точки, куда смотрит прицел\n" +
                   "• point save <название> — сохранить текущую точку под именем\n" +
                   "• point list — вывести список всех сохраненных точек\n" +
                   "• point tool — выдать Тулган Спавнпоинтов (ToolGun) в руки";
        return false;
    }
}
