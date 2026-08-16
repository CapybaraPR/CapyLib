namespace Capy.Core.Extensions;

/// <summary>
/// Полезные методы расширения для класса Player.
/// </summary>
public static class PlayerExtensions
{
    public static bool IsAliveAndValid(this Player? player)
    {
        return player != null && player.IsAlive && player.Role.Type != PlayerRoles.RoleTypeId.None && player.Role.Type != PlayerRoles.RoleTypeId.Spectator;
    }

    public static void SetTemporaryVariable<T>(this Player player, string key, T value)
    {
        if (player == null || string.IsNullOrEmpty(key)) return;
        player.SessionVariables[key] = value!;
    }

    public static bool TryGetTemporaryVariable<T>(this Player player, string key, out T? value)
    {
        value = default;
        if (player == null || string.IsNullOrEmpty(key)) return false;

        if (player.SessionVariables.TryGetValue(key, out var raw) && raw is T typedVal)
        {
            value = typedVal;
            return true;
        }
        return false;
    }

    public static void RemoveTemporaryVariable(this Player player, string key)
    {
        if (player == null || string.IsNullOrEmpty(key)) return;
        player.SessionVariables.Remove(key);
    }
}
