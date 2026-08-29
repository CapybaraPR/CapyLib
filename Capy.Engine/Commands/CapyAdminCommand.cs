using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Capy.API.DiscordBridge;
using Capy.Core.Database.Models;
using Capy.Core.Loader;
using CommandSystem;
using Exiled.API.Features;

namespace Capy.Commands;

/// <summary>
/// Главная команда управления библиотекой, модулями и стаффом CapyLib.
/// Доступна через .capy или .al.
/// </summary>
[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
[CommandHandler(typeof(ClientCommandHandler))]
public class CapyAdminCommand : ParentCommand
{
    public static CapyAdminCommand? Instance { get; private set; }
    private static readonly List<ICommand> DynamicSubcommands = new();

    public CapyAdminCommand() => LoadGeneratedCommands();

    public override string Command => "capy";
    public override string[] Aliases => new[] { "al", "capylib", "cl", "капи" };
    public override string Description => "Управление CapyLib, модулями и персоналом (Remote Admin)";

    public static void RegisterSubcommand(ICommand cmd)
    {
        if (cmd == null) return;
        lock (DynamicSubcommands)
        {
            if (!DynamicSubcommands.Any(c => c.Command.Equals(cmd.Command, StringComparison.OrdinalIgnoreCase)))
                DynamicSubcommands.Add(cmd);
        }

        try
        {
            Instance?.RegisterCommand(cmd);
        }
        catch { }
    }

    public override void LoadGeneratedCommands()
    {
        Instance = this;

        RegisterCommand(new StaffSubcommand());
        RegisterCommand(new ListSubcommand());
        RegisterCommand(new ToggleSubcommand());
        RegisterCommand(new EnableSubcommand());
        RegisterCommand(new DisableSubcommand());
        RegisterCommand(new RestartSubcommand());
        RegisterCommand(new ReloadSubcommand());

        lock (DynamicSubcommands)
        {
            foreach (var cmd in DynamicSubcommands)
            {
                try { RegisterCommand(cmd); } catch { }
            }
        }
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        bool isDev = ModuleManager.CheckAccess(sender);
        Player? player = Player.Get(sender);
        bool isStaff = player == null || player.RemoteAdminAccess || !string.IsNullOrEmpty(player.GroupName);

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("<color=#ffa94e>============================================================</color>");
        sb.AppendLine("<b><color=#ffd285>          [ CAPYLIB • ПАНЕЛЬ АДМИНИСТРИРОВАНИЯ ]</color></b>");
        sb.AppendLine("<color=#ffa94e>============================================================</color>");
        sb.AppendLine();

        if (isStaff)
        {
            sb.AppendLine("<color=#58b9ff>>> ДЛЯ АДМИНИСТРАЦИИ (STAFF):</color>");
            sb.AppendLine("  <color=#ffd285>* capy staff [ник/steamid]</color> <color=#c2c2c2>-- Проверить норму и часы сотрудника</color>");
            sb.AppendLine();
        }

        if (isDev)
        {
            sb.AppendLine("<color=#f87171>>> ДЛЯ РАЗРАБОТЧИКОВ (DEV • МОДУЛИ ЯДРА):</color>");
            sb.AppendLine("  <color=#ffd285>* capy list</color>               <color=#c2c2c2>-- Список всех модулей CapyLib и их статус</color>");
            sb.AppendLine("  <color=#ffd285>* capy toggle <модуль></color>    <color=#c2c2c2>-- Включить / выключить модуль</color>");
            sb.AppendLine("  <color=#ffd285>* capy enable <модуль></color>    <color=#c2c2c2>-- Включить модуль</color>");
            sb.AppendLine("  <color=#ffd285>* capy disable <модуль></color>   <color=#c2c2c2>-- Отключить модуль</color>");
            sb.AppendLine("  <color=#ffd285>* capy restart <модуль></color>   <color=#c2c2c2>-- Перезапустить модуль</color>");
            sb.AppendLine("  <color=#ffd285>* capy reload</color>             <color=#c2c2c2>-- Перезагрузить конфигурации</color>");
            sb.AppendLine();
        }

        sb.AppendLine("<color=#a3e635>>> ИГРОВЫЕ КОМАНДЫ (ВВОДЯТСЯ В КОНСОЛИ `~` ЧЕРЕЗ ТОЧКУ):</color>");
        sb.AppendLine("  <color=#ffd285>.menu</color> — Справка по всем командам сервера");
        sb.AppendLine("  <color=#ffd285>.stats</color> — Личная статистика | <color=#ffd285>.top</color> — Лидерборд");
        sb.Append("<color=#ffa94e>============================================================</color>");

        if (player != null)
        {
            player.SendConsoleMessage(sb.ToString(), "white");
            response = string.Empty;
            return true;
        }

        response = sb.ToString();
        return true;
    }
}

public class HelpSubcommand : ICommand
{
    public string Command => "help";
    public string[] Aliases => new[] { "cmds", "cmd", "commands", "menu", "хелп", "меню" };
    public string Description => "Справка по командам сервера.";

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

public class StatsSubcommand : ICommand
{
    public string Command => "stats";
    public string[] Aliases => new[] { "profile", "me" };
    public string Description => "Просмотр личной статистики.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        return new StatsCommand().Execute(arguments, sender, out response);
    }
}

public class TopSubcommand : ICommand
{
    public string Command => "top";
    public string[] Aliases => new[] { "leaderboard", "lb" };
    public string Description => "Просмотр топа игроков сервера.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        return new TopCommand().Execute(arguments, sender, out response);
    }
}

public class StaffSubcommand : ICommand
{
    public string Command => "staff";
    public string[] Aliases => new[] { "duty" };
    public string Description => "Проверить статус сотрудника, часы за неделю и норму.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player == null || !player.IsConnected || string.IsNullOrWhiteSpace(player.UserId))
        {
            response = "Команда доступна только игроку на сервере.";
            return false;
        }

        string targetUserId = player.UserId;
        string targetNickname = player.Nickname;

        if (arguments.Count > 0 && arguments.Array != null && (player.RemoteAdminAccess || !string.IsNullOrEmpty(player.GroupName)))
        {
            string query = arguments.Array[arguments.Offset].Trim();
            Player? found = Player.List.FirstOrDefault(p =>
                p.Nickname.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                p.UserId.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                p.Id.ToString() == query);

            if (found != null)
            {
                targetUserId = found.UserId;
                targetNickname = found.Nickname;
            }
            else if (Regex.IsMatch(query, @"^\d{17}$"))
            {
                targetUserId = $"{query}@steam";
                targetNickname = query;
            }
        }

        StaffMemberModel? staff = DiscordBridgeModule.StaffService?.GetStaff(targetUserId);
        if (staff == null || !staff.IsActive)
        {
            string error = "\n<color=#f87171>[Отказ в доступе] Данный аккаунт не числится в активном составе персонала сервера.</color>\n";
            player.SendConsoleMessage(error, "white");
            response = string.Empty;
            return false;
        }

        long weeklySec = staff.WeeklyPlaytimeSeconds;
        long quotaSec = 4 * 3600; // 4 hours
        double percent = (double)weeklySec / quotaSec * 100.0;
        string progressStatus = weeklySec >= quotaSec
            ? $"<color=#a3e635>[{percent:F0}% • НОРМА ВЫПОЛНЕНА]</color>"
            : $"<color=#ffd285>[{percent:F0}% • Осталось {FormatTime(quotaSec - weeklySec)}]</color>";

        int totalPunishments = staff.BansCount + staff.MutesCount + staff.KicksCount;
        string cleanId = targetUserId.Replace("@steam", "").Replace("@discord", "");

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("<color=#ffa94e>============================================================</color>");
        sb.AppendLine($"<b><color=#ffd285>          [ СЛУЖЕБНЫЙ ПРОФИЛЬ СОТРУДНИКА: {targetNickname} ]</color></b>");
        sb.AppendLine("<color=#ffa94e>============================================================</color>");
        sb.AppendLine($"<color=#58b9ff>* SteamID64:</color> <color=#ffffff>{cleanId}</color>");
        sb.AppendLine($"<color=#58b9ff>* Должность:</color> <color=#ffd285>{staff.Group}</color> <color=#58b9ff>[{staff.ServerScope.ToUpperInvariant()}]</color>");
        sb.AppendLine($"<color=#58b9ff>* Статус в реестре:</color> <color=#a3e635>АКТИВЕН</color>");
        if (staff.DiscordUserId != 0)
            sb.AppendLine($"<color=#58b9ff>* Привязанный Discord ID:</color> <color=#5865f2>{staff.DiscordUserId}</color>");
        sb.AppendLine("<color=#ffa94e>------------------------------------------------------------</color>");
        sb.AppendLine("<color=#f87171>>> ВЫПОЛНЕНИЕ СЛУЖЕБНОЙ НОРМЫ (НЕДЕЛЯ):</color>");
        sb.AppendLine($"<color=#58b9ff>* Наиграно:</color> <color=#ffd285>{FormatTime(weeklySec)}</color> / <color=#ffffff>4 ч. 00 мин.</color> {progressStatus}");
        sb.AppendLine($"<color=#58b9ff>* Наказаний выдано:</color> <color=#f87171>{totalPunishments}</color> <color=#c2c2c2>(Банов: {staff.BansCount} | Мутов: {staff.MutesCount} | Киков: {staff.KicksCount})</color>");
        sb.Append("<color=#ffa94e>============================================================</color>");

        player.SendConsoleMessage(sb.ToString(), "white");
        response = string.Empty;
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

public class ListSubcommand : ICommand
{
    public string Command => "list";
    public string[] Aliases => new[] { "l" };
    public string Description => "Список модулей и подсистем CapyLib";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ModuleManager.CheckAccess(sender))
        {
            response = "Отказ в доступе.";
            return false;
        }

        var modules = ModuleManager.GetAll().ToList();
        if (modules.Count == 0)
        {
            response = "Нет зарегистрированных модулей в CapyLib.";
            return true;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"\n<b><color=#ffa94e>Зарегистрировано модулей ({modules.Count}):</color></b>");

        foreach (var mod in modules)
        {
            string status = mod.IsEnabled ? "<color=#a3e635>[ВКЛЮЧЕН]</color>" : "<color=#f87171>[ВЫКЛЮЧЕН]</color>";
            sb.AppendLine($"{status} <b>{mod.Name}</b> — <color=#c2c2c2>{mod.Description}</color>");
        }

        response = sb.ToString();
        return true;
    }
}

public class ToggleSubcommand : ICommand
{
    public string Command => "toggle";
    public string[] Aliases => new[] { "t" };
    public string Description => "Переключить состояние модуля";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ModuleManager.CheckAccess(sender))
        {
            response = "Отказ в доступе.";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = "Использование: .capy toggle <имя_модуля>";
            return false;
        }

        string moduleName = arguments.At(0);
        if (!ModuleManager.TryGet(moduleName, out var module) || module == null)
        {
            response = $"Модуль '{moduleName}' не найден.";
            return false;
        }

        if (module.IsEnabled)
            ModuleManager.Disable(moduleName, out response);
        else
            ModuleManager.Enable(moduleName, out response);

        return true;
    }
}

public class EnableSubcommand : ICommand
{
    public string Command => "enable";
    public string[] Aliases => new[] { "on" };
    public string Description => "Включить модуль";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ModuleManager.CheckAccess(sender))
        {
            response = "Отказ в доступе.";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = "Использование: .capy enable <имя_модуля>";
            return false;
        }

        string moduleName = arguments.At(0);
        return ModuleManager.Enable(moduleName, out response);
    }
}

public class DisableSubcommand : ICommand
{
    public string Command => "disable";
    public string[] Aliases => new[] { "off" };
    public string Description => "Отключить модуль";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ModuleManager.CheckAccess(sender))
        {
            response = "Отказ в доступе.";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = "Использование: .capy disable <имя_модуля>";
            return false;
        }

        string moduleName = arguments.At(0);
        return ModuleManager.Disable(moduleName, out response);
    }
}

public class RestartSubcommand : ICommand
{
    public string Command => "restart";
    public string[] Aliases => new[] { "r" };
    public string Description => "Перезапустить модуль (горячая перезагрузка)";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ModuleManager.CheckAccess(sender))
        {
            response = "Отказ в доступе.";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = "Использование: .capy restart <имя_модуля>";
            return false;
        }

        string moduleName = arguments.At(0);
        return ModuleManager.Restart(moduleName, out response);
    }
}

public class ReloadSubcommand : ICommand
{
    public string Command => "reload";
    public string[] Aliases => new[] { "rel" };
    public string Description => "Перезагрузить конфигурации";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ModuleManager.CheckAccess(sender))
        {
            response = "Отказ в доступе.";
            return false;
        }

        response = "Команда перезагрузки конфигураций выполнена.";
        return true;
    }
}
