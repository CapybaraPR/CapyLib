using System;
using System.Text;
using CommandSystem;
using Exiled.API.Features;

namespace Capy.Commands;

public static class HelpMessageBuilder
{
    public static string Build(ICommandSender? sender)
    {
        Player? player = sender != null ? Player.Get(sender) : null;
        bool isStaff = player != null && (player.RemoteAdminAccess || !string.IsNullOrEmpty(player.GroupName));

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("============================================================");
        sb.AppendLine("              [ КАПИБАРА SCP:SL • СПИСОК КОМАНД ]");
        sb.AppendLine("============================================================");
        sb.AppendLine();
        sb.AppendLine(">> ОСНОВНЫЕ КОМАНДЫ ДЛЯ ИГРОКОВ:");
        sb.AppendLine("  * .stats                 -- Личная статистика (K/D, раунды, онлайн)");
        sb.AppendLine("  * .top                   -- Топ-10 игроков сервера по фрагам");
        sb.AppendLine("  * .top time              -- Топ-10 игроков по наигранному времени");
        sb.AppendLine("  * .linkdiscord <код>     -- Привязать Discord к аккаунту (/steamsl)");
        sb.AppendLine("  * .help                  -- Показать эту справку");
        sb.AppendLine();
        sb.AppendLine(">> ТЕГИ И ОТОБРАЖЕНИЕ:");
        sb.AppendLine("  * .showtag               -- Показать свой тег над головой");
        sb.AppendLine("  * .hidetag               -- Скрыть свой тег над головой");
        sb.AppendLine("  * .globaltag             -- Показать глобальный значок");
        sb.AppendLine();

        if (isStaff)
        {
            sb.AppendLine(">> ДЛЯ АДМИНИСТРАЦИИ (STAFF):");
            sb.AppendLine("  * .capy staff            -- Проверить свои часы за неделю и норму");
            sb.AppendLine("  * .overwatch (или .ow)   -- Включить/выключить режим наблюдения");
            sb.AppendLine();
        }

        sb.AppendLine("============================================================");
        sb.AppendLine(">> Наш Discord сервер: discord.gg/capybara");
        sb.AppendLine("============================================================");

        return sb.ToString();
    }
}

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class HelpCommand : ICommand
{
    public string Command { get; } = "help";
    public string[] Aliases { get; } = { "хелп", "помощь", "команды", "cmds", "menu" };
    public string Description { get; } = "Список доступных команд сервера.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        string msg = HelpMessageBuilder.Build(sender);
        Player? player = Player.Get(sender);
        if (player != null)
        {
            player.SendConsoleMessage(msg, "#ffa94e");
            response = string.Empty;
            return true;
        }

        response = msg;
        return true;
    }
}
