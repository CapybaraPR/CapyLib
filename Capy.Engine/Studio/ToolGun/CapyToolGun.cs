using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using Capy.Engine.Hints;
using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Extensions;
using Capy.Engine.Hud.Panels;
using Capy.Engine.ServerSpecific;
using Capy.Engine.Studio.Core;
using Capy.Engine.Studio.Models;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using Exiled.API.Features.Toys;
using Exiled.Events.EventArgs.Player;
using MEC;
using UnityEngine;
using Light = Exiled.API.Features.Toys.Light;

namespace Capy.Engine.Studio.ToolGun;

public enum ToolGunMode
{
    Spawn = 0,
    PhysGun = 1,
    Rotate = 2,
    Scale = 3,
    Delete = 4
}

public enum ToolCategory
{
    Primitives = 0,
    Lights = 1,
    Doors = 2,
    Schematics = 3
}

/// <summary>
/// Состояние ToolGun для каждого игрока.
/// </summary>
public sealed class ToolGunState
{
    public ToolGunMode Mode { get; set; } = ToolGunMode.Spawn;
    public ToolCategory Category { get; set; } = ToolCategory.Primitives;
    public int SelectedIndex { get; set; } = 0;
    public float GridSnap { get; set; } = 0.5f; // 0 = Free
    public float RotationStep { get; set; } = 45f;
    public float CurrentScale { get; set; } = 1.0f;
    public string CurrentColorHex { get; set; } = "#FFFFFF";

    // Состояние живого захвата (PhysGun)
    public bool IsGrabbing { get; set; }
    public float HoldDistance { get; set; } = 3.5f;
    public Quaternion HoldRelativeRotation { get; set; } = Quaternion.identity;

    public Primitive? GrabbedPrimitive { get; set; }
    public SchematicObject? GrabbedSchematic { get; set; }
    public Light? GrabbedLight { get; set; }
    public Pickup? GrabbedPickup { get; set; }
    public CoroutineHandle GrabCoroutine { get; set; }
}

/// <summary>
/// Строительный инструмент строителя (ToolGun & PhysGun).
/// Оснащён лазерным позиционированием, живым захватом объектов на лету (Real-time Grab & Drag),
/// сеткой привязки, поддержкой событий выстрела/прицеливания оружия и нативным HUD-управлением.
/// </summary>
public static class CapyToolGun
{
    private static readonly ConcurrentDictionary<int, ToolGunState> States = new();
    private static readonly ConcurrentDictionary<int, DateTime> LastPrimaryAction = new();
    private static readonly ConcurrentDictionary<int, DateTime> LastSecondaryAction = new();
    private const double ActionDedupWindowMs = 150;

    private static readonly PrimitiveType[] AvailablePrimitives = { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Cylinder, PrimitiveType.Capsule, PrimitiveType.Quad };
    private static readonly string[] AvailableColors = { "#FFFFFF", "#FF3333", "#33FF33", "#3388FF", "#FFAA00", "#AA33FF", "#825228", "#333333" };

    /// <summary>
    /// Гасит дубли: одно физическое нажатие ЛКМ/ПКМ приходит и как событие оружия (EXILED),
    /// и как серверный SS-бинд. Второе срабатывание в пределах окна игнорируется.
    /// </summary>
    private static bool IsDuplicatePress(ConcurrentDictionary<int, DateTime> map, int playerId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime last = map.GetOrAdd(playerId, DateTime.MinValue);

        if ((now - last).TotalMilliseconds < ActionDedupWindowMs)
            return true;

        map[playerId] = now;
        return false;
    }

    public static void Initialize()
    {
        AssKeybinds.OnKeybindPressed += OnKeybindPressed;
        ItemHudPanel.ExternalItemHudProvider += GetToolGunHudText;

        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.DryfiringWeapon += OnDryfiringWeapon;
        Exiled.Events.Handlers.Player.AimingDownSight += OnAimingDownSight;
        Exiled.Events.Handlers.Player.ReloadingWeapon += OnReloadingWeapon;
        Exiled.Events.Handlers.Player.TogglingFlashlight += OnTogglingFlashlight;
        Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
    }

    public static void Unregister()
    {
        AssKeybinds.OnKeybindPressed -= OnKeybindPressed;
        ItemHudPanel.ExternalItemHudProvider -= GetToolGunHudText;
        Exiled.Events.Handlers.Player.Shooting -= OnShooting;
        Exiled.Events.Handlers.Player.DryfiringWeapon -= OnDryfiringWeapon;
        Exiled.Events.Handlers.Player.AimingDownSight -= OnAimingDownSight;
        Exiled.Events.Handlers.Player.ReloadingWeapon -= OnReloadingWeapon;
        Exiled.Events.Handlers.Player.TogglingFlashlight -= OnTogglingFlashlight;
        Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;

        foreach (var state in States.Values)
        {
            if (state.GrabCoroutine.IsRunning)
                Timing.KillCoroutines(state.GrabCoroutine);
        }
        States.Clear();
        LastPrimaryAction.Clear();
        LastSecondaryAction.Clear();
    }

    /// <summary>
    /// Возвращает общее (разделяемое между командами/биндами/HUD) состояние ToolGun игрока.
    /// </summary>
    public static ToolGunState GetOrCreateState(Player player)
    {
        return States.GetOrAdd(player.Id, _ => new ToolGunState());
    }

    public static bool IsHoldingToolGun(Player? player)
    {
        if (player == null || !player.IsConnected || player.CurrentItem == null)
            return false;

        return player.CurrentItem.Type == ItemType.GunCOM15 && States.ContainsKey(player.Id);
    }

    public static bool ToggleToolGun(Player player, out string response)
    {
        if (States.TryGetValue(player.Id, out var existing))
        {
            if (existing.GrabCoroutine.IsRunning)
                Timing.KillCoroutines(existing.GrabCoroutine);

            States.TryRemove(player.Id, out _);
            response = "<color=yellow>[TOOLGUN]</color> Режим строителя <b>выключен</b>.";
            return true;
        }
        else
        {
            States[player.Id] = new ToolGunState();

            if (player.CurrentItem?.Type != ItemType.GunCOM15)
            {
                player.AddItem(ItemType.GunCOM15);
            }

            response = "<color=green>[TOOLGUN]</color> Режим строителя <b>активирован</b>!\nВозьмите пистолет <color=#ffa94e>COM-15</color> в руки.";
            return true;
        }
    }

    private static void OnShooting(ShootingEventArgs ev)
    {
        if (!IsHoldingToolGun(ev.Player)) return;

        ev.IsAllowed = false;
        if (IsDuplicatePress(LastPrimaryAction, ev.Player.Id)) return;
        ExecutePrimaryAction(ev.Player, GetOrCreateState(ev.Player));
    }

    private static void OnDryfiringWeapon(DryfiringWeaponEventArgs ev)
    {
        if (!IsHoldingToolGun(ev.Player)) return;

        ev.IsAllowed = false;
        if (IsDuplicatePress(LastPrimaryAction, ev.Player.Id)) return;
        ExecutePrimaryAction(ev.Player, GetOrCreateState(ev.Player));
    }

    private static void OnAimingDownSight(AimingDownSightEventArgs ev)
    {
        if (!IsHoldingToolGun(ev.Player)) return;

        if (IsDuplicatePress(LastSecondaryAction, ev.Player.Id)) return;
        ExecuteSecondaryAction(ev.Player, GetOrCreateState(ev.Player));
    }

    private static void OnReloadingWeapon(ReloadingWeaponEventArgs ev)
    {
        if (!IsHoldingToolGun(ev.Player)) return;

        ev.IsAllowed = false;
        var state = States.GetOrAdd(ev.Player.Id, _ => new ToolGunState());

        if (state.IsGrabbing)
            RotateHeldObject(ev.Player, state);
        else
            CycleCategory(ev.Player, state);
    }

    private static void OnTogglingFlashlight(TogglingFlashlightEventArgs ev)
    {
        if (!IsHoldingToolGun(ev.Player)) return;

        var state = States.GetOrAdd(ev.Player.Id, _ => new ToolGunState());

        if (state.IsGrabbing)
            AdjustHoldDistance(ev.Player, state);
        else
            CycleMode(ev.Player, state);
    }

    private static void OnDroppingItem(DroppingItemEventArgs ev)
    {
        if (ev.Item?.Type == ItemType.GunCOM15 && IsHoldingToolGun(ev.Player))
        {
            ev.IsAllowed = false;
            var state = States.GetOrAdd(ev.Player.Id, _ => new ToolGunState());

            if (state.IsGrabbing)
            {
                ReleaseGrab(ev.Player, state);
                ev.Player.ShowZoneHint(HintZone.Notification, "<color=#facc15>Объект опущен на место.</color>", 1.5f, "tg_grab", 20);
            }
            else
            {
                ExecuteDeleteAction(ev.Player, state);
            }
        }
    }

    private static void OnKeybindPressed(Player player, CustomKeybind keybind)
    {
        if (!IsHoldingToolGun(player)) return;

        var state = States.GetOrAdd(player.Id, _ => new ToolGunState());

        switch (keybind)
        {
            case CustomKeybind.Lmb:
                if (IsDuplicatePress(LastPrimaryAction, player.Id)) return;
                ExecutePrimaryAction(player, state);
                break;

            case CustomKeybind.Rmb:
                if (IsDuplicatePress(LastSecondaryAction, player.Id)) return;
                ExecuteSecondaryAction(player, state);
                break;

            case CustomKeybind.F:
                if (state.IsGrabbing)
                    RotateHeldObject(player, state);
                else
                    CycleMode(player, state);
                break;

            case CustomKeybind.T:
                if (state.IsGrabbing)
                    AdjustHoldDistance(player, state);
                else
                    CycleCategory(player, state);
                break;

            case CustomKeybind.G:
                if (state.IsGrabbing)
                {
                    ReleaseGrab(player, state);
                    player.ShowZoneHint(HintZone.Notification, "<color=#facc15>Объект опущен на место.</color>", 1.5f, "tg_grab", 20);
                }
                else
                {
                    ExecuteDeleteAction(player, state);
                }
                break;
        }
    }

    public static void ExecutePrimaryAction(Player player, ToolGunState state)
    {
        if (state.IsGrabbing)
        {
            LockGrabbedObject(player, state);
            return;
        }

        if (!GetAimPoint(player, state.GridSnap, out var hitPoint, out var hitNormal))
            return;

        switch (state.Mode)
        {
            case ToolGunMode.Spawn:
                SpawnSelectedObject(player, state, hitPoint, hitNormal);
                break;

            case ToolGunMode.PhysGun:
                TryStartGrab(player, state);
                break;

            case ToolGunMode.Delete:
                ExecuteDeleteAction(player, state);
                break;
        }
    }

    public static void ExecuteSecondaryAction(Player player, ToolGunState state)
    {
        if (state.IsGrabbing)
        {
            ReleaseGrab(player, state);
            player.ShowZoneHint(HintZone.Notification, "<color=#facc15>Объект зафиксирован.</color>", 1.5f, "tg_grab", 20);
            return;
        }

        TryStartGrab(player, state);
    }

    public static bool TryStartGrab(Player player, ToolGunState state, string? targetSchematicName = null)
    {
        Vector3 eyePos = player.CameraTransform.position;
        Vector3 forward = player.CameraTransform.forward;

        // 1. Поиск по точному лучу (RaycastAll)
        var hits = Physics.RaycastAll(eyePos, forward, 50f);
        foreach (var hit in hits.OrderBy(h => h.distance))
        {
            // Проверка примитивов
            var primObj = hit.collider.GetComponentInParent<PrimitiveObjectToy>();
            if (primObj != null)
            {
                var prim = Primitive.Get(primObj);
                if (prim != null)
                {
                    var schem = SchematicLoader.SpawnedSchematics.FirstOrDefault(s => s.SpawnedPrimitives.Contains(prim) || (s.RootObject != null && hit.collider.transform.IsChildOf(s.RootObject.transform)));
                    if (schem != null)
                    {
                        StartGrabbingSchematic(player, state, schem);
                        return true;
                    }

                    StartGrabbingPrimitive(player, state, prim);
                    return true;
                }
            }

            // Проверка источников света
            var lightObj = hit.collider.GetComponentInParent<LightSourceToy>();
            if (lightObj != null)
            {
                var light = Light.Get(lightObj);
                if (light != null)
                {
                    StartGrabbingLight(player, state, light);
                    return true;
                }
            }

            // Проверка предметов на полу
            var pickupBase = hit.collider.GetComponentInParent<InventorySystem.Items.Pickups.ItemPickupBase>();
            if (pickupBase != null)
            {
                var pickup = Pickup.Get(pickupBase);
                if (pickup != null)
                {
                    StartGrabbingPickup(player, state, pickup);
                    return true;
                }
            }
        }

        // 2. Поиск ближайшей схематики по конусу взгляда (если примитивы тонкие или без коллизии)
        SchematicObject? bestSchem = null;
        float bestAngle = 35f;

        foreach (var schem in SchematicLoader.SpawnedSchematics)
        {
            if (schem.IsDestroyed) continue;
            if (!string.IsNullOrEmpty(targetSchematicName) && !schem.Name.Equals(targetSchematicName, StringComparison.OrdinalIgnoreCase))
                continue;

            Vector3 dir = schem.Position - eyePos;
            float dist = dir.magnitude;
            if (dist > 30f) continue;

            float angle = Vector3.Angle(forward, dir);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                bestSchem = schem;
            }
        }

        if (bestSchem != null)
        {
            StartGrabbingSchematic(player, state, bestSchem);
            return true;
        }

        player.ShowZoneHint(HintZone.Notification, "<color=#ff4444>Наведите прицел на объект или схематику.</color>", 1.5f, "tg_grab", 20);
        return false;
    }

    public static void StartGrabbingPrimitive(Player player, ToolGunState state, Primitive prim)
    {
        ReleaseGrab(player, state);

        state.IsGrabbing = true;
        state.GrabbedPrimitive = prim;
        state.HoldDistance = Mathf.Clamp(Vector3.Distance(player.CameraTransform.position, prim.Position), 1.5f, 40f);
        state.HoldRelativeRotation = Quaternion.Inverse(player.Rotation) * prim.Rotation;

        state.GrabCoroutine = Timing.RunCoroutine(RealtimeGrabCoroutine(player, state));

        player.ShowZoneHint(HintZone.Notification, $"<color=#38bdf8>🧲 Захвачен примитив <b>{prim.Type}</b>!</color>\n<size=14><color=#c2c2c2>[ЛКМ]: Поставить • [F/R]: Повернуть • [T]: Дистанция</color></size>", 3.0f, "tg_grab", 20);
    }

    public static void StartGrabbingSchematic(Player player, ToolGunState state, SchematicObject schem)
    {
        ReleaseGrab(player, state);

        state.IsGrabbing = true;
        state.GrabbedSchematic = schem;
        state.HoldDistance = Mathf.Clamp(Vector3.Distance(player.CameraTransform.position, schem.Position), 2.0f, 40f);
        state.HoldRelativeRotation = Quaternion.Inverse(player.Rotation) * schem.Rotation;

        state.GrabCoroutine = Timing.RunCoroutine(RealtimeGrabCoroutine(player, state));

        player.ShowZoneHint(HintZone.Notification, $"<color=#38bdf8>🧲 Захвачена вся схематика <b>{schem.Name}</b>!</color>\n<size=14><color=#c2c2c2>[ЛКМ]: Поставить • [F/R]: Повернуть • [T]: Дистанция</color></size>", 3.0f, "tg_grab", 20);
    }

    public static void StartGrabbingLight(Player player, ToolGunState state, Light light)
    {
        ReleaseGrab(player, state);

        state.IsGrabbing = true;
        state.GrabbedLight = light;
        state.HoldDistance = Mathf.Clamp(Vector3.Distance(player.CameraTransform.position, light.Position), 1.5f, 40f);
        state.HoldRelativeRotation = Quaternion.Inverse(player.Rotation) * light.Rotation;

        state.GrabCoroutine = Timing.RunCoroutine(RealtimeGrabCoroutine(player, state));

        player.ShowZoneHint(HintZone.Notification, "<color=#ffd285>🧲 Захвачен Источник Света!</color>", 2.5f, "tg_grab", 20);
    }

    public static void StartGrabbingPickup(Player player, ToolGunState state, Pickup pickup)
    {
        ReleaseGrab(player, state);

        state.IsGrabbing = true;
        state.GrabbedPickup = pickup;
        state.HoldDistance = Mathf.Clamp(Vector3.Distance(player.CameraTransform.position, pickup.Position), 1.5f, 40f);
        state.HoldRelativeRotation = Quaternion.Inverse(player.Rotation) * pickup.Rotation;

        state.GrabCoroutine = Timing.RunCoroutine(RealtimeGrabCoroutine(player, state));

        player.ShowZoneHint(HintZone.Notification, $"<color=#a3e635>🧲 Захвачен предмет <b>{pickup.Type}</b>!</color>", 2.5f, "tg_grab", 20);
    }

    private static IEnumerator<float> RealtimeGrabCoroutine(Player player, ToolGunState state)
    {
        while (player != null && player.IsConnected && state.IsGrabbing && IsHoldingToolGun(player))
        {
            try
            {
                Vector3 targetPos = player.CameraTransform.position + (player.CameraTransform.forward * state.HoldDistance);
                Quaternion targetRot = player.Rotation * state.HoldRelativeRotation;

                if (state.GridSnap > 0.01f)
                {
                    targetPos = new Vector3(
                        Mathf.Round(targetPos.x / state.GridSnap) * state.GridSnap,
                        Mathf.Round(targetPos.y / state.GridSnap) * state.GridSnap,
                        Mathf.Round(targetPos.z / state.GridSnap) * state.GridSnap
                    );
                }

                if (state.GrabbedPrimitive != null)
                {
                    state.GrabbedPrimitive.Position = targetPos;
                    state.GrabbedPrimitive.Rotation = targetRot;
                }
                else if (state.GrabbedSchematic != null)
                {
                    state.GrabbedSchematic.SetTransform(targetPos, targetRot);
                }
                else if (state.GrabbedLight != null)
                {
                    state.GrabbedLight.Position = targetPos;
                    state.GrabbedLight.Rotation = targetRot;
                }
                else if (state.GrabbedPickup != null && state.GrabbedPickup.GameObject != null)
                {
                    state.GrabbedPickup.Position = targetPos;
                    state.GrabbedPickup.Rotation = targetRot;
                }
                else
                {
                    break;
                }
            }
            catch
            {
                break;
            }

            yield return Timing.WaitForOneFrame;
        }

        ReleaseGrab(player, state);
    }

    private static void RotateHeldObject(Player player, ToolGunState state)
    {
        state.HoldRelativeRotation *= Quaternion.Euler(0, state.RotationStep, 0);
        player.ShowZoneHint(HintZone.Notification, $"<color=#38bdf8>Поворот: <b>+{state.RotationStep}°</b></color>", 1.0f, "tg_rot", 20);
    }

    private static void AdjustHoldDistance(Player player, ToolGunState state)
    {
        state.HoldDistance += 1.0f;
        if (state.HoldDistance > 12.0f) state.HoldDistance = 2.0f;

        player.ShowZoneHint(HintZone.Notification, $"<color=#facc15>Дистанция: <b>{state.HoldDistance:F1}м</b></color>", 1.0f, "tg_dist", 20);
    }

    private static void LockGrabbedObject(Player player, ToolGunState state)
    {
        ReleaseGrab(player, state);
        player.ShowZoneHint(HintZone.Notification, "<color=#a3e635>🔒 Объект зафиксирован на месте!</color>", 1.5f, "tg_lock", 20);
    }

    public static void ReleaseGrab(Player? player, ToolGunState state)
    {
        state.IsGrabbing = false;
        if (state.GrabCoroutine.IsRunning)
            Timing.KillCoroutines(state.GrabCoroutine);

        state.GrabbedPrimitive = null;
        state.GrabbedSchematic = null;
        state.GrabbedLight = null;
        state.GrabbedPickup = null;
    }

    private static void SpawnSelectedObject(Player player, ToolGunState state, Vector3 hitPoint, Vector3 hitNormal)
    {
        Quaternion spawnRot = Quaternion.LookRotation(hitNormal == Vector3.up ? player.CameraTransform.forward : hitNormal);

        switch (state.Category)
        {
            case ToolCategory.Primitives:
            {
                var primType = AvailablePrimitives[state.SelectedIndex % AvailablePrimitives.Length];
                Color color = ColorUtility.TryParseHtmlString(state.CurrentColorHex, out var c) ? c : Color.white;

                var prim = Primitive.Create(
                    primitiveType: primType,
                    flags: PrimitiveFlags.Visible | PrimitiveFlags.Collidable,
                    position: hitPoint,
                    rotation: spawnRot.eulerAngles,
                    scale: Vector3.one * state.CurrentScale,
                    spawn: true,
                    color: color
                );

                state.GrabbedPrimitive = prim;
                player.ShowZoneHint(HintZone.Notification, $"<color=#a3e635>Заспавнен <b>{primType}</b>!</color>", 1.5f, "tg_spawn", 20);
                break;
            }

            case ToolCategory.Lights:
            {
                Color color = ColorUtility.TryParseHtmlString(state.CurrentColorHex, out var c) ? c : Color.white;
                var light = Light.Create(
                    position: hitPoint + hitNormal * 0.2f,
                    rotation: spawnRot.eulerAngles,
                    scale: Vector3.one,
                    spawn: false,
                    color: color
                );

                if (light != null)
                {
                    light.Intensity = 1.5f;
                    light.Range = 8f;
                    light.Spawn();
                }

                player.ShowZoneHint(HintZone.Notification, "<color=#ffd285>Заспавнен <b>Источник Света</b>!</color>", 1.5f, "tg_spawn", 20);
                break;
            }

            case ToolCategory.Schematics:
            {
                var schematics = SchematicLoader.GetAvailableSchematics();
                if (schematics.Count == 0)
                {
                    player.ShowZoneHint(HintZone.Notification, "<color=#ff4444>Нет доступных схематик в Schematics/</color>", 2.0f, "tg_spawn", 20);
                    return;
                }

                string name = schematics[state.SelectedIndex % schematics.Count];
                var schem = SchematicLoader.Spawn(name, hitPoint, spawnRot, Vector3.one * state.CurrentScale);
                state.GrabbedSchematic = schem;

                player.ShowZoneHint(HintZone.Notification, $"<color=#a3e635>Заспавнена схематика <b>{name}</b>!</color>", 2.0f, "tg_spawn", 20);
                break;
            }
        }
    }

    private static void ExecuteDeleteAction(Player player, ToolGunState state)
    {
        if (Physics.Raycast(player.CameraTransform.position, player.CameraTransform.forward, out var hit, 40f))
        {
            var primObj = hit.collider.GetComponentInParent<PrimitiveObjectToy>();
            if (primObj != null)
            {
                var prim = Primitive.Get(primObj);
                if (prim != null)
                {
                    var schem = SchematicLoader.SpawnedSchematics.FirstOrDefault(s => s.SpawnedPrimitives.Contains(prim));
                    if (schem != null)
                    {
                        SchematicLoader.RemoveInstance(schem);
                        player.ShowZoneHint(HintZone.Notification, $"<color=#ff4444>Схематика <b>{schem.Name}</b> удалена целиком.</color>", 1.5f, "tg_del", 20);
                        return;
                    }

                    prim.Destroy();
                    player.ShowZoneHint(HintZone.Notification, "<color=#ff4444>Примитив удалён.</color>", 1.5f, "tg_del", 20);
                    return;
                }
            }

            var lightObj = hit.collider.GetComponentInParent<LightSourceToy>();
            if (lightObj != null)
            {
                var light = Light.Get(lightObj);
                light?.Destroy();
                player.ShowZoneHint(HintZone.Notification, "<color=#ff4444>Источник Света удалён.</color>", 1.5f, "tg_del", 20);
                return;
            }
        }
    }

    private static void CycleMode(Player player, ToolGunState state)
    {
        state.Mode = (ToolGunMode)(((int)state.Mode + 1) % 5);
        player.ShowZoneHint(HintZone.Notification, $"<color=#38bdf8>Режим: <b>{state.Mode}</b></color>", 1.5f, "tg_mode", 20);
    }

    private static void CycleCategory(Player player, ToolGunState state)
    {
        state.Category = (ToolCategory)(((int)state.Category + 1) % 4);
        state.SelectedIndex = 0;
        player.ShowZoneHint(HintZone.Notification, $"<color=#ffa94e>Категория: <b>{state.Category}</b></color>", 1.5f, "tg_cat", 20);
    }

    private static bool GetAimPoint(Player player, float gridSnap, out Vector3 point, out Vector3 normal)
    {
        if (Physics.Raycast(player.CameraTransform.position, player.CameraTransform.forward, out var hit, 60f))
        {
            point = hit.point;
            normal = hit.normal;

            if (gridSnap > 0.01f)
            {
                point = new Vector3(
                    Mathf.Round(point.x / gridSnap) * gridSnap,
                    Mathf.Round(point.y / gridSnap) * gridSnap,
                    Mathf.Round(point.z / gridSnap) * gridSnap
                );
            }

            return true;
        }

        point = Vector3.zero;
        normal = Vector3.up;
        return false;
    }

    private static string? GetToolGunHudText(Player player)
    {
        if (!IsHoldingToolGun(player)) return null;

        var state = States.GetOrAdd(player.Id, _ => new ToolGunState());

        if (state.IsGrabbing)
        {
            string heldName = state.GrabbedSchematic != null
                ? $"Схематика: {state.GrabbedSchematic.Name}"
                : state.GrabbedPrimitive != null
                    ? $"Примитив: {state.GrabbedPrimitive.Type}"
                    : state.GrabbedPickup != null
                        ? $"Предмет: {state.GrabbedPickup.Type}"
                        : "Объект";

            return $"<size=20><color=#38bdf8><b>[ CapyStudio PhysGun ]</b></color></size>\n" +
                   $"<size=15><color=#a3e635>🧲 Захвачено: <b>{heldName}</b></color> • Дист: <color=#fcd34d><b>{state.HoldDistance:F1}м</b></color> [T/Фонарик]\n" +
                   $"<color=#ffffff>[ЛКМ/Выстрел]: Поставить • [ПКМ/Прицел]: Захват • [F/R]: Поворот</color></size>";
        }

        string selectedName = state.Category switch
        {
            ToolCategory.Primitives => AvailablePrimitives[state.SelectedIndex % AvailablePrimitives.Length].ToString(),
            ToolCategory.Lights => "Источник Света",
            ToolCategory.Doors => "Дверь",
            ToolCategory.Schematics => GetCurrentSchematicName(state),
            _ => "Объект"
        };

        string snapStr = state.GridSnap <= 0.01f ? "Выкл" : $"{state.GridSnap:F2}м";

        return $"<size=20><color=#38bdf8><b>[ CapyStudio ToolGun ]</b></color></size>\n" +
               $"<size=15><color=#ffffff>Режим: <color=#a3e635><b>{state.Mode}</b></color> • Категория: <color=#ffa94e><b>{state.Category}</b></color> [R]\n" +
               $"Выбрано: <color=#67e8f9><b>{selectedName}</b></color> • Сетка: <color=#fcd34d><b>{snapStr}</b></color> • Масштаб: <color=#f472b6><b>{state.CurrentScale:F1}x</b></color>\n" +
               $"<color=#c2c2c2>[ЛКМ]: Спавн • [ПКМ]: 🧲 Захват лучом (PhysGun) • [G]: Удалить</color></size>";
    }

    private static string GetCurrentSchematicName(ToolGunState state)
    {
        var list = SchematicLoader.GetAvailableSchematics();
        if (list.Count == 0) return "Нет файлов";
        return list[state.SelectedIndex % list.Count];
    }
}
