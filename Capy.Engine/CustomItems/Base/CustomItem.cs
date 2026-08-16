using Capy.Engine.CustomItems.Models;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using UnityEngine;

namespace Capy.Engine.CustomItems.Base;

/// <summary>
/// Базовый абстрактный класс для создания кастомных предметов на серверах Capybara.
/// </summary>
public abstract class CustomItem
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract ItemType BaseType { get; }

    public virtual string ColorHex { get; } = "#FFA500";
    public virtual Vector3 Scale { get; } = Vector3.one;
    public virtual ItemRarity Rarity { get; } = ItemRarity.Common;
    public virtual float Weight { get; } = 1f;
    public virtual List<ItemAbility> Abilities { get; } = new();

    public readonly HashSet<ushort> TrackedSerials = new();

    public virtual void OnRegistered() { }
    public virtual void OnUnregistered() { }

    public virtual void OnPickedUp(Player player, Pickup pickup) { }
    public virtual void OnDropped(Player player, Pickup pickup) { }
    public virtual void OnHolding(Player player, Item item) { }
    public virtual void OnStopHolding(Player player, Item item) { }
    public virtual void OnShot(Player player, Item item) { }

    public bool IsTracked(ushort serial) => TrackedSerials.Contains(serial);
    public bool IsTracked(Item item) => item != null && TrackedSerials.Contains(item.Serial);
    public bool IsTracked(Pickup pickup) => pickup != null && TrackedSerials.Contains(pickup.Serial);

    public virtual Item? Give(Player player)
    {
        var item = player.AddItem(BaseType);
        if (item != null)
        {
            TrackedSerials.Add(item.Serial);
        }
        return item;
    }

    public virtual Pickup? Spawn(Vector3 position, Quaternion rotation)
    {
        var pickup = Pickup.CreateAndSpawn(BaseType, position, rotation);
        if (pickup != null)
        {
            TrackedSerials.Add(pickup.Serial);
            pickup.Scale = Scale;
        }
        return pickup;
    }
}
