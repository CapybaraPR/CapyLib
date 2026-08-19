using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using CommandSystem.Commands.Shared;
using HarmonyLib;
using Utils;

namespace Capy.Engine.Patches;

[HarmonyPatch(typeof(CommandSystem.Commands.Console.LineHelpCommand), nameof(CommandSystem.Commands.Console.LineHelpCommand.Execute))]
internal static class LineHelpCommandPatch
{
    internal static bool Prefix(ArraySegment<string> arguments, ICommandSender sender, ref string response)
    {
        response = Capy.Commands.HelpMessageBuilder.Build(sender);
        return false;
    }
}

[HarmonyPatch(typeof(HelpCommand), nameof(HelpCommand.Execute))]
internal static class HelpCommandPatch
{
    internal static bool Prefix(HelpCommand __instance, ArraySegment<string> arguments, ICommandSender sender, ref string response)
    {
        response = Capy.Commands.HelpMessageBuilder.Build(sender);
        return false;
    }
}

[HarmonyPatch(typeof(KeyCommand), nameof(KeyCommand.Execute))]
internal static class KeyCommandPatch
{
    internal static bool Prefix(KeyCommand __instance, ArraySegment<string> arguments,
        ICommandSender sender, ref string response)
    {
        response = "Данная команда отключена в целях безопасности";
        return false;
    }
}

[HarmonyPatch(typeof(SetGroupCommand), nameof(SetGroupCommand.Execute))]
internal static class SetGroupCommandPatch
{
    internal static bool Prefix(SetGroupCommand __instance, ArraySegment<string> arguments,
        ICommandSender sender, ref string response)
    {
        if (!sender.CheckPermission(PlayerPermissions.SetGroup, out response))
        {
            return true;
        }

        if (arguments.Count < 2)
        {
            return true;
        }

        List<ReferenceHub> playersToAffect = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out string[] array);

        if (array[0].Contains("ruk."))
        {
            response = "Вы не можете выдать руководящие должности через игру";
            return false;
        }

        if (playersToAffect.Count <= 1) return true;
        
        response = "Вы не можете выдать группу больше чем одному человеку за раз";
        return false;
    }
}

[HarmonyPatch(typeof(SetGroupCommand), nameof(SetGroupCommand.Execute))]
internal static class PmSetGroupCommandPatch
{
    internal static bool Prefix(SetGroupCommand __instance, ArraySegment<string> arguments,
        ICommandSender sender, ref string response)
    {
        if (!sender.CheckPermission(PlayerPermissions.PermissionsManagement, out response))
        {
            return true;
        }

        if (arguments.Count < 2)
        {
            return true;
        }

        if (!arguments.At(1).Contains("ruk.")) return true;
        
        response = "Вы не можете выдать руководящие должности через игру";
        return false;
    }
}

[HarmonyPatch(typeof(ReloadConfigCommand), nameof(ReloadConfigCommand.Execute))]
internal static class ReloadConfigCommandPatch
{
    internal static bool Prefix(ReloadConfigCommand __instance, ArraySegment<string> arguments,
        ICommandSender sender, ref string response)
    {
        if (Player.Get(sender) == null)
        {
            response = "Данную команду нельзя выполнять в игре";
            return false;
        }
        
        return true;
    }
}

[HarmonyPatch(typeof(RconCommand), nameof(RconCommand.Execute))]
internal static class SudoCommandPatch
{
    internal static bool Prefix(RconCommand __instance, ArraySegment<string> arguments,
        ICommandSender sender, ref string response)
    {
        response = "Команда отключена в целях безопасности";
        return false;
    }
}
