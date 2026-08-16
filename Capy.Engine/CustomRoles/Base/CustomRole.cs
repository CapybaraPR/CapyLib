using Exiled.API.Enums;
using PlayerRoles;

namespace Capy.Engine.CustomRoles.Base;

/// <summary>
/// Базовый абстрактный класс для создания кастомных ролей игроков.
/// </summary>
public abstract class CustomRole
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract RoleTypeId BaseRole { get; }

    public virtual float MaxHealth { get; set; } = 100f;
    public virtual float HumeShield { get; set; } = 0f;
    public virtual List<ItemType> StartingItems { get; set; } = new();
    public virtual Dictionary<AmmoType, ushort> StartingAmmo { get; set; } = new();

    public readonly HashSet<string> TrackedPlayers = new();

    public virtual void OnRegistered() { }
    public virtual void OnUnregistered() { }

    public virtual void OnAssigned(Player player)
    {
        TrackedPlayers.Add(player.UserId);

        player.Role.Set(BaseRole);
        player.MaxHealth = MaxHealth;
        player.Health = MaxHealth;
        player.HumeShield = HumeShield;

        player.ClearInventory();
        foreach (var item in StartingItems)
        {
            player.AddItem(item);
        }

        foreach (var (ammoType, amount) in StartingAmmo)
        {
            player.SetAmmo(ammoType, amount);
        }
    }

    public virtual void OnRemoved(Player player)
    {
        TrackedPlayers.Remove(player.UserId);
    }

    public bool IsPlayerRole(Player player) => player != null && TrackedPlayers.Contains(player.UserId);
}
