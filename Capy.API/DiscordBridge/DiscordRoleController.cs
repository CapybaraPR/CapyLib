using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;

namespace Capy.API.DiscordBridge;

public sealed class DiscordRoleController
{
    private readonly DiscordBridgeConfig _config;
    private readonly DiscordLinkService _links;
    private readonly HashSet<string> _warnedMissingGroups = new(StringComparer.OrdinalIgnoreCase);
    private CoroutineHandle _loop;

    public DiscordRoleController(DiscordBridgeConfig config, DiscordLinkService links)
    {
        _config = config;
        _links = links;
    }

    public void Start() => _loop = Timing.RunCoroutine(UpdateLoop());

    public void Stop()
    {
        if (_loop.IsRunning)
            Timing.KillCoroutines(_loop);
    }

    public void ApplyToOnlinePlayers()
    {
        foreach (Player player in Player.List.ToList())
        {
            try
            {
                ApplyToPlayer(player);
            }
            catch (Exception ex)
            {
                Log.Warn($"[DiscordBridge.RoleController] Ошибка применения Discord-роли игроку: {ex.Message}");
            }
        }
    }

    public void ApplyToPlayer(Player player)
    {
        if (player == null || !player.IsConnected || player.IsHost || player.IsNPC ||
            !player.IsVerified || string.IsNullOrWhiteSpace(player.UserId))
        {
            return;
        }

        if (!_links.TryGetByGameUserId(player.UserId, out DiscordLinkRecord? account) || account == null)
            return;

        if (IsProtectedGroup(player))
            return;

        HashSet<ulong> memberRoles = (account.DiscordRoleIds ?? new List<ulong>()).ToHashSet();
        DiscordRoleMapping? match = (_config.DiscordRoleMappings ?? new List<DiscordRoleMapping>())
            .Where(IsUsable)
            .OrderByDescending(mapping => mapping.Priority)
            .FirstOrDefault(mapping => Matches(mapping, memberRoles));

        if (match == null)
        {
            if (_config.DiscordRoleRemoveManagedGroupWhenNoMatch && HasManagedGroup(player))
            {
                player.Group = null!;
                player.RankName = null;
                player.RankColor = null;
                _links.UpdateManagedGroup(player.UserId, null);
                Log.Info($"[DiscordBridge.RoleController] Снята управляемая Discord-группа у {player.Nickname} ({player.UserId}): подходящих ролей больше нет.");
            }
            return;
        }

        UserGroup? targetGroup = ServerStatic.PermissionsHandler.GetGroup(match.Group.Trim());
        if (targetGroup == null)
        {
            if (_warnedMissingGroups.Add(match.Group))
                Log.Warn($"[DiscordBridge.RoleController] EXILED-группа '{match.Group}' из discord_role_mappings не найдена.");
            return;
        }

        if (ReferenceEquals(player.Group, targetGroup))
            return;

        player.Group = targetGroup;
        _links.UpdateManagedGroup(player.UserId, match.Group);
        Log.Info($"[DiscordBridge.RoleController] Игроку {player.Nickname} ({player.UserId}) применена EXILED-группа '{match.Group}' по Discord-ролям.");
    }

    private IEnumerator<float> UpdateLoop()
    {
        while (true)
        {
            ApplyToOnlinePlayers();

            float interval = Math.Max(1f, Math.Min(_config.DiscordRoleCheckIntervalSeconds, 300f));
            yield return Timing.WaitForSeconds(interval);
        }
    }

    private bool IsProtectedGroup(Player player)
    {
        string currentGroupName = player.GroupName ?? string.Empty;
        foreach (string ignored in _config.DiscordRoleIgnoredGroups ?? new List<string>())
        {
            string name = (ignored ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (currentGroupName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;

            UserGroup? group = ServerStatic.PermissionsHandler.GetGroup(name);
            if (group != null && ReferenceEquals(player.Group, group))
                return true;
        }

        return false;
    }

    private bool HasManagedGroup(Player player)
    {
        string currentGroupName = player.GroupName ?? string.Empty;
        foreach (DiscordRoleMapping mapping in _config.DiscordRoleMappings ?? new List<DiscordRoleMapping>())
        {
            if (!IsUsable(mapping))
                continue;

            string name = mapping.Group.Trim();
            if (currentGroupName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;

            UserGroup? group = ServerStatic.PermissionsHandler.GetGroup(name);
            if (group != null && ReferenceEquals(player.Group, group))
                return true;
        }

        return false;
    }

    private static bool IsUsable(DiscordRoleMapping mapping) =>
        mapping != null &&
        !string.IsNullOrWhiteSpace(mapping.Group) &&
        (mapping.DiscordRoleIds?.Any(id => id != 0) ?? false);

    private static bool Matches(DiscordRoleMapping mapping, HashSet<ulong> memberRoles)
    {
        List<ulong> required = (mapping.DiscordRoleIds ?? new List<ulong>())
            .Where(id => id != 0)
            .Distinct()
            .ToList();
        if (required.Count == 0)
            return false;

        return (mapping.Operator ?? "or").Trim().Equals("and", StringComparison.OrdinalIgnoreCase)
            ? required.All(memberRoles.Contains)
            : required.Any(memberRoles.Contains);
    }
}
