using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using CommandSystem.Commands.Shared;
using HarmonyLib;
using Utils;

namespace Capy.Engine.Patches;

[HarmonyPatch(typeof(global::RemoteAdmin.QueryProcessor), "ProcessGameConsoleQuery")]
internal static class ProcessGameConsoleQueryPatch
{
    internal static bool Prefix(global::RemoteAdmin.QueryProcessor __instance, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        string q = query.Trim();
        if (q.Equals("help", StringComparison.OrdinalIgnoreCase) ||
            q.Equals(".help", StringComparison.OrdinalIgnoreCase) ||
            q.Equals("хелп", StringComparison.OrdinalIgnoreCase) ||
            q.Equals(".хелп", StringComparison.OrdinalIgnoreCase) ||
            q.Equals("помощь", StringComparison.OrdinalIgnoreCase) ||
            q.Equals(".помощь", StringComparison.OrdinalIgnoreCase) ||
            q.Equals("команды", StringComparison.OrdinalIgnoreCase) ||
            q.Equals(".команды", StringComparison.OrdinalIgnoreCase))
        {
            Exiled.API.Features.Player? player = Exiled.API.Features.Player.Get(__instance.gameObject);
            string msg = Capy.Commands.HelpMessageBuilder.Build(player?.Sender);
            player?.SendConsoleMessage(msg, "white");
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(global::RemoteAdmin.QueryProcessor), "ParseCommandsToStruct")]
internal static class ParseCommandsToStructPatch
{
    private static readonly HashSet<string> IgnoredCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "audiopooldebug",
        "srvcfg",
        "contact",
        "hello",
        "exiledtag",
        "useability",
        "groups"
    };

    internal static void Postfix(ref global::RemoteAdmin.QueryProcessor.CommandData[] __result)
    {
        if (__result == null) return;
        var filtered = new List<global::RemoteAdmin.QueryProcessor.CommandData>();
        foreach (var cmd in __result)
        {
            if (!string.IsNullOrEmpty(cmd.Command) && IgnoredCommands.Contains(cmd.Command))
                continue;

            filtered.Add(cmd);
        }
        __result = filtered.ToArray();
    }
}

[HarmonyPatch(typeof(global::RemoteAdmin.CommandProcessor), "ProcessQuery")]
internal static class CommandProcessorHelpPatch
{
    internal static bool Prefix(string q, CommandSender sender, ref string __result)
    {
        if (string.IsNullOrWhiteSpace(q)) return true;
        string trimmed = q.Trim();
        if (trimmed.Equals("help", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals(".help", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("хелп", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals(".хелп", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("помощь", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals(".помощь", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("команды", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals(".команды", StringComparison.OrdinalIgnoreCase))
        {
            __result = Capy.Commands.HelpMessageBuilder.Build(sender);
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(CommandSystem.Commands.Console.LineHelpCommand), nameof(CommandSystem.Commands.Console.LineHelpCommand.Execute))]
internal static class LineHelpCommandPatch
{
    internal static bool Prefix(ArraySegment<string> arguments, ICommandSender sender, ref string response, ref bool __result)
    {
        response = Capy.Commands.HelpMessageBuilder.Build(sender);
        __result = true;
        return false;
    }
}

[HarmonyPatch(typeof(HelpCommand), nameof(HelpCommand.Execute))]
internal static class HelpCommandPatch
{
    internal static bool Prefix(HelpCommand __instance, ArraySegment<string> arguments, ICommandSender sender, ref string response, ref bool __result)
    {
        response = Capy.Commands.HelpMessageBuilder.Build(sender);
        __result = true;
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
