using CommandSystem;
using Exiled.Permissions.Extensions;

namespace Capy.Core.Features;

public abstract class PermittedCommand : ICommand {
    public abstract string Command { get; }
    public abstract string[] Aliases { get; }
    public abstract string Description { get; }
    
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response) {
        if (sender.CheckPermission($"{this.Command}") || sender.CheckPermission("*")) return this.OnExecuted(arguments, sender, out response);
        
        response = "Недостаточно прав!";
        return false;
    }

    protected abstract bool OnExecuted(ArraySegment<string> arguments, ICommandSender sender, out string response);
}
