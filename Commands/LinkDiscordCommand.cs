using System;
using Capy.API.DiscordBridge;
using CommandSystem;
using Exiled.API.Features;

namespace Capy.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class LinkDiscordCommand : ICommand
{
    public string Command { get; } = "linkdiscord";
    public string[] Aliases { get; } = { "linkds", "discordlink" };
    public string Description { get; } = "Привязать Discord к игровому аккаунту по коду из бота (/steamsl).";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player == null || !player.IsConnected || player.IsNPC ||
            !player.IsVerified || string.IsNullOrWhiteSpace(player.UserId))
        {
            response = "Команда доступна только авторизованному игроку на сервере.";
            return false;
        }

        if (arguments.Count != 1 || arguments.Array == null)
        {
            response = "Использование: .linkdiscord <6-значный код>\nПример: .linkdiscord 123456";
            return false;
        }

        DiscordLinkService? linkService = DiscordBridgeModule.LinkService;
        if (linkService == null)
        {
            response = "Модуль интеграции с Discord сейчас выключен или недоступен на сервере.";
            return false;
        }

        string code = arguments.Array[arguments.Offset];
        if (!linkService.TryRedeemCode(code, player.UserId, out DiscordLinkRecord? account, out string error) || account == null)
        {
            response = error;
            return false;
        }

        DiscordBridgeModule.RoleController?.ApplyToPlayer(player);
        response = $"Аккаунт Discord '{account.DiscordUserName}' успешно привязан к вашему профилю! Ваши роли и привилегии обновлены.";
        return true;
    }
}
