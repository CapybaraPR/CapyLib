using Capy.Engine.CustomItems.Base;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp914;

namespace Capy.Engine.CustomItems.Manager;

/// <summary>
/// Центральный реестр и менеджер жизненного цикла кастомных предметов.
/// </summary>
public static class CustomItemsManager
{
    private static readonly List<CustomItem> RegisteredItems = new();
    private static bool _isSubscribed;

    public static IReadOnlyList<CustomItem> Items => RegisteredItems.AsReadOnly();

    public static void Register(CustomItem item)
    {
        if (item == null || RegisteredItems.Contains(item)) return;

        RegisteredItems.Add(item);
        item.OnRegistered();
        Log.Info($"[CustomItemsManager] Зарегистрирован кастомный предмет: {item.Name}");

        EnsureEvents();
    }

    public static void UnregisterAll()
    {
        foreach (var item in RegisteredItems)
        {
            try
            {
                item.OnUnregistered();
                item.TrackedSerials.Clear();
            }
            catch { }
        }
        RegisteredItems.Clear();

        if (_isSubscribed)
        {
            Exiled.Events.Handlers.Player.PickingUpItem -= OnPickingUpItem;
            Exiled.Events.Handlers.Player.DroppedItem -= OnDroppedItem;
            Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
            Exiled.Events.Handlers.Scp914.UpgradingPickup -= OnUpgradingPickup;
            _isSubscribed = false;
        }
    }

    private static void EnsureEvents()
    {
        if (_isSubscribed) return;

        Exiled.Events.Handlers.Player.PickingUpItem += OnPickingUpItem;
        Exiled.Events.Handlers.Player.DroppedItem += OnDroppedItem;
        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Scp914.UpgradingPickup += OnUpgradingPickup;
        _isSubscribed = true;
    }

    public static CustomItem? GetBySerial(ushort serial)
    {
        return RegisteredItems.FirstOrDefault(i => i.IsTracked(serial));
    }

    private static void OnPickingUpItem(PickingUpItemEventArgs ev)
    {
        if (ev.Pickup == null || ev.Player == null) return;

        var customItem = GetBySerial(ev.Pickup.Serial);
        if (customItem != null)
        {
            customItem.OnPickedUp(ev.Player, ev.Pickup);
        }
    }

    private static void OnDroppedItem(DroppedItemEventArgs ev)
    {
        if (ev.Pickup == null || ev.Player == null) return;

        var customItem = GetBySerial(ev.Pickup.Serial);
        if (customItem != null)
        {
            customItem.OnDropped(ev.Player, ev.Pickup);
        }
    }

    private static void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Player == null) return;

        if (ev.OldItem != null)
        {
            var oldCustom = GetBySerial(ev.OldItem.Serial);
            oldCustom?.OnStopHolding(ev.Player, ev.OldItem);
        }

        if (ev.Item != null)
        {
            var newCustom = GetBySerial(ev.Item.Serial);
            newCustom?.OnHolding(ev.Player, ev.Item);
        }
    }

    private static void OnUpgradingPickup(UpgradingPickupEventArgs ev)
    {
        if (ev.Pickup == null) return;

        var customItem = GetBySerial(ev.Pickup.Serial);
        if (customItem != null)
        {
            // Кастомные предметы защищены от случайной перезаписи SCP-914
            ev.IsAllowed = false;
        }
    }
}
