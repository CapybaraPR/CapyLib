using Capy.Engine.CustomRoles.Base;
using Exiled.Events.EventArgs.Player;

namespace Capy.Engine.CustomRoles.Manager;

/// <summary>
/// Центральный реестр и менеджер кастомных ролей.
/// </summary>
public static class CustomRolesManager
{
    private static readonly List<CustomRole> RegisteredRoles = new();
    private static bool _isSubscribed;

    public static IReadOnlyList<CustomRole> Roles => RegisteredRoles.AsReadOnly();

    public static void Register(CustomRole role)
    {
        if (role == null || RegisteredRoles.Contains(role)) return;

        RegisteredRoles.Add(role);
        role.OnRegistered();
        Log.Info($"[CustomRolesManager] Зарегистрирована кастомная роль: {role.Name}");

        EnsureEvents();
    }

    public static void UnregisterAll()
    {
        foreach (var role in RegisteredRoles)
        {
            try
            {
                role.OnUnregistered();
                role.TrackedPlayers.Clear();
            }
            catch { }
        }
        RegisteredRoles.Clear();

        if (_isSubscribed)
        {
            Exiled.Events.Handlers.Player.Died -= OnDied;
            Exiled.Events.Handlers.Player.ChangingRole -= OnChangingRole;
            _isSubscribed = false;
        }
    }

    private static void EnsureEvents()
    {
        if (_isSubscribed) return;

        Exiled.Events.Handlers.Player.Died += OnDied;
        Exiled.Events.Handlers.Player.ChangingRole += OnChangingRole;
        _isSubscribed = true;
    }

    public static CustomRole? GetRole(Player player)
    {
        if (player == null) return null;
        return RegisteredRoles.FirstOrDefault(r => r.IsPlayerRole(player));
    }

    private static void OnDied(DiedEventArgs ev)
    {
        if (ev.Player == null) return;
        var role = GetRole(ev.Player);
        role?.OnRemoved(ev.Player);
    }

    private static void OnChangingRole(ChangingRoleEventArgs ev)
    {
        if (ev.Player == null) return;
        var role = GetRole(ev.Player);
        role?.OnRemoved(ev.Player);
    }
}
