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
        sb.AppendLine("<color=#ffa94e>============================================================</color>");
        sb.AppendLine("<b><color=#ffd285>              [ КАПИБАРА SCP:SL • СПИСОК КОМАНД ]</color></b>");
        sb.AppendLine("<color=#ffa94e>============================================================</color>");
        sb.AppendLine();
        sb.AppendLine("<color=#58b9ff>>> ОСНОВНЫЕ КОМАНДЫ ДЛЯ ИГРОКОВ:</color>");
        sb.AppendLine("  <color=#ffd285>* .stats</color>                 <color=#c2c2c2>-- Личная статистика (K/D, раунды, онлайн)</color>");
        sb.AppendLine("  <color=#ffd285>* .top</color>                   <color=#c2c2c2>-- Топ-10 игроков сервера по фрагам</color>");
        sb.AppendLine("  <color=#ffd285>* .top time</color>              <color=#c2c2c2>-- Топ-10 игроков по наигранному времени</color>");
        sb.AppendLine("  <color=#ffd285>* .res</color>                   <color=#c2c2c2>-- Быстрое возрождение в первые 3 мин (наблюдатели)</color>");
        sb.AppendLine("  <color=#ffd285>* .kill</color>                  <color=#c2c2c2>-- Совершить самоубийство (живые игроки)</color>");
        sb.AppendLine("  <color=#ffd285>* .linkdiscord <код></color>     <color=#c2c2c2>-- Привязать Discord к аккаунту (/steamsl)</color>");
        sb.AppendLine("  <color=#ffd285>* .help</color>                  <color=#c2c2c2>-- Показать эту справку</color>");
        sb.AppendLine();
        sb.AppendLine("<color=#a3e635>>> ТЕГИ И ОТОБРАЖЕНИЕ:</color>");
        sb.AppendLine("  <color=#ffd285>* .showtag</color>               <color=#c2c2c2>-- Показать свой тег над головой</color>");
        sb.AppendLine("  <color=#ffd285>* .hidetag</color>               <color=#c2c2c2>-- Скрыть свой тег над головой</color>");
        sb.AppendLine("  <color=#ffd285>* .globaltag</color>             <color=#c2c2c2>-- Показать глобальный значок</color>");
        sb.AppendLine();

        if (isStaff)
        {
            sb.AppendLine("<color=#f87171>>> ДЛЯ АДМИНИСТРАЦИИ (STAFF):</color>");
            sb.AppendLine("  <color=#ffd285>* .capy staff</color>            <color=#c2c2c2>-- Проверить свои часы за неделю и норму</color>");
            sb.AppendLine("  <color=#ffd285>* .overwatch (или .ow)</color>   <color=#c2c2c2>-- Включить/выключить режим наблюдения</color>");
            sb.AppendLine();
        }

        sb.AppendLine("<color=#ffa94e>============================================================</color>");
        sb.AppendLine("<color=#ffa94e>>> Наш Discord сервер: </color><color=#5865f2>discord.gg/capybara</color>");
        sb.Append("<color=#ffa94e>============================================================</color>");

        return sb.ToString();
    }
}

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class HelpCommand : ICommand
{
    public string Command { get; } = "help";
    public string[] Aliases { get; } = { "cmds", "menu" };
    public string Description { get; } = "Список доступных команд сервера.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        string msg = HelpMessageBuilder.Build(sender);
        Player? player = Player.Get(sender);
        if (player != null)
        {
            player.SendConsoleMessage(msg, "white");
            response = string.Empty;
            return true;
        }

        response = msg;
        return true;
    }
}
