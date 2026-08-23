using CommandSystem;

namespace Capy.Engine.DevTools.Models;

public abstract class PermittedCommand : ICommand {
    public abstract string Command { get; }
    public abstract string[] Aliases { get; }
    public abstract string Description { get; }
    public abstract PlayerPermissions RequiredPermissions { get; }
    
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response) {
        if (sender.CheckPermission(this.RequiredPermissions)) {
            return this.ExecutePermitted(arguments, sender, out response);
        }

        response = "Недостаточно прав!";
        return false;
    }

    protected abstract bool ExecutePermitted(ArraySegment<string> arguments, ICommandSender sender, out string response);
}