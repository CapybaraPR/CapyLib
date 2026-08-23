using System;
using System.Linq;
using Capy.Engine.CustomItems.Manager;
using CommandSystem;
using Exiled.API.Features;

namespace Capy.Engine.DevTools.SpawnPoints.Commands;

public class PointToolSubcommand : ICommand
{
    public string Command => "tool";
    public string[] Aliases => new[] { "give", "item" };
    public string Description => "Выдать предмет Тулгана Спавнпоинтов (ToolGun) админу в инвентарь.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player == null || player.ReferenceHub == null)
        {
            response = "Вы должны быть игроком на сервере!";
            return false;
        }

        var toolGun = CustomItemsManager.Items.FirstOrDefault(i => i.Name.Equals("ToolGun", StringComparison.OrdinalIgnoreCase));
        if (toolGun == null)
        {
            response = "Кастомный предмет ToolGun не зарегистрирован!";
            return false;
        }

        toolGun.Give(player);
        response = "Вам выдан ToolGun для настройки спавнпоинтов.";
        return true;
    }
}
