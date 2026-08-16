using System.Text;
using Capy.Core.Loader;
using CommandSystem;

namespace Capy.Commands;

/// <summary>
/// Главная команда управления библиотекой, модулями и плагинами CapyLib.
/// Доступна через .capy или .al (для обратной совместимости).
/// </summary>
[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public class CapyAdminCommand : ParentCommand
{
    public CapyAdminCommand() => LoadGeneratedCommands();

    public override string Command => "capy";
    public override string[] Aliases => new[] { "al", "capylib", "cl" };
    public override string Description => "Управление модулями и плагинами CapyLib (только для разработчиков)";

    public override void LoadGeneratedCommands()
    {
        RegisterCommand(new ListSubcommand());
        RegisterCommand(new ToggleSubcommand());
        RegisterCommand(new EnableSubcommand());
        RegisterCommand(new DisableSubcommand());
        RegisterCommand(new RestartSubcommand());
        RegisterCommand(new ReloadSubcommand());
    }

    protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ModuleManager.CheckAccess(sender))
        {
            response = "\n<color=#ff3333><b>[Отказ в доступе]</b> У вас нет прав для управления CapyLib.\n" +
                       "Команда доступна только консоли сервера и авторизованным разработчикам.</color>";
            return false;
        }

        var sb = new StringBuilder();
        sb.AppendLine("\n<b><color=#ffa500>══════════════ [ CAPYLIB УПРАВЛЕНИЕ ] ══════════════</color></b>");
        sb.AppendLine("<b><color=#00ffff>• .capy list</color></b> — Список всех зарегистрированных модулей и статус");
        sb.AppendLine("<b><color=#00ffff>• .capy toggle <имя></color></b> — Переключить состояние модуля");
        sb.AppendLine("<b><color=#00ffff>• .capy enable <имя></color></b> — Включить модуль");
        sb.AppendLine("<b><color=#00ffff>• .capy disable <имя></color></b> — Отключить модуль");
        sb.AppendLine("<b><color=#00ffff>• .capy restart <имя></color></b> — Перезапустить модуль (горячая перезагрузка)");
        sb.AppendLine("<b><color=#00ffff>• .capy reload</color></b> — Перезагрузить все YAML-конфигурации");
        sb.AppendLine("<b><color=#ffa500>═════════════════════════════════════════════════</color></b>");

        response = sb.ToString();
        return true;
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
        sb.AppendLine($"\n<b><color=#ffa500>Зарегистрировано модулей ({modules.Count}):</color></b>");

        foreach (var mod in modules)
        {
            string status = mod.IsEnabled ? "<color=#00ff00>[ВКЛЮЧЕН]</color>" : "<color=#ff3333>[ВЫКЛЮЧЕН]</color>";
            sb.AppendLine($"{status} <b>{mod.Name}</b> — <color=#cccccc>{mod.Description}</color>");
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

        return ModuleManager.Toggle(arguments.At(0), out response);
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

        return ModuleManager.Enable(arguments.At(0), out response);
    }
}

public class DisableSubcommand : ICommand
{
    public string Command => "disable";
    public string[] Aliases => new[] { "off" };
    public string Description => "Выключить модуль";

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

        return ModuleManager.Disable(arguments.At(0), out response);
    }
}

public class RestartSubcommand : ICommand
{
    public string Command => "restart";
    public string[] Aliases => new[] { "r" };
    public string Description => "Перезапустить модуль";

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

        return ModuleManager.Restart(arguments.At(0), out response);
    }
}

public class ReloadSubcommand : ICommand
{
    public string Command => "reload";
    public string[] Aliases => new[] { "rel" };
    public string Description => "Перезагрузить все YAML конфиги";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ModuleManager.CheckAccess(sender))
        {
            response = "Отказ в доступе.";
            return false;
        }

        return ModuleManager.ReloadAllConfigs(out response);
    }
}
