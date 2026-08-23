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
using Exiled.API.Features.Doors;
using Exiled.API.Features.Toys;
using UnityEngine;
using Light = Exiled.API.Features.Toys.Light;

namespace Capy.Engine.Studio.ToolGun;

public enum ToolGunMode
{
    Spawn = 0,
    Move = 1,
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

    public Primitive? SelectedPrimitive { get; set; }
    public SchematicObject? SelectedSchematic { get; set; }
}

/// <summary>
/// Строительный инструмент строителя (ToolGun).
/// Оснащён лазерным позиционированием, сеткой привязки и нативным HUD-управлением.
/// </summary>
public static class CapyToolGun
{
    private static readonly ConcurrentDictionary<int, ToolGunState> States = new();
    private static readonly PrimitiveType[] AvailablePrimitives = { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Cylinder, PrimitiveType.Capsule, PrimitiveType.Quad };
    private static readonly string[] AvailableColors = { "#FFFFFF", "#FF3333", "#33FF33", "#3388FF", "#FFAA00", "#AA33FF", "#825228", "#333333" };

    public static void Initialize()
    {
        AssKeybinds.OnKeybindPressed += OnKeybindPressed;
        ItemHudPanel.ExternalItemHudProvider += GetToolGunHudText;
    }

    public static void Unregister()
    {
        AssKeybinds.OnKeybindPressed -= OnKeybindPressed;
        States.Clear();
    }

    public static bool IsHoldingToolGun(Player? player)
    {
        if (player == null || !player.IsConnected || player.CurrentItem == null)
            return false;

        return player.CurrentItem.Type == ItemType.GunCOM15 && States.ContainsKey(player.Id);
    }

    public static bool ToggleToolGun(Player player, out string response)
    {
        if (States.ContainsKey(player.Id))
        {
            States.TryRemove(player.Id, out _);
            response = "<color=yellow>[TOOLGUN]</color> Режим строителя <b>выключен</b>.";
            return true;
        }
        else
        {
            States[player.Id] = new ToolGunState();

            // Выдаем COM-15 если нет в руках
            if (player.CurrentItem?.Type != ItemType.GunCOM15)
            {
                player.AddItem(ItemType.GunCOM15);
            }

            response = "<color=green>[TOOLGUN]</color> Режим строителя <b>активирован</b>!\nВозьмите пистолет <color=#ffa94e>COM-15</color> в руки.";
            return true;
        }
    }

    private static void OnKeybindPressed(Player player, CustomKeybind keybind)
    {
        if (!IsHoldingToolGun(player)) return;

        var state = States.GetOrAdd(player.Id, _ => new ToolGunState());

        switch (keybind)
        {
            case CustomKeybind.Lmb:
                ExecutePrimaryAction(player, state);
                break;

            case CustomKeybind.Rmb:
                ExecuteSecondaryAction(player, state);
                break;

            case CustomKeybind.F: // Inspect -> Cycle Mode
                CycleMode(player, state);
                break;

            case CustomKeybind.T: // Cycle Category
                CycleCategory(player, state);
                break;

            case CustomKeybind.G: // Delete / Clear
                ExecuteDeleteAction(player, state);
                break;
        }
    }

    private static void ExecutePrimaryAction(Player player, ToolGunState state)
    {
        if (!GetAimPoint(player, state.GridSnap, out var hitPoint, out var hitNormal))
            return;

        switch (state.Mode)
        {
            case ToolGunMode.Spawn:
                SpawnSelectedObject(player, state, hitPoint, hitNormal);
                break;

            case ToolGunMode.Move:
                if (state.SelectedPrimitive != null)
                {
                    state.SelectedPrimitive.Position = hitPoint;
                    player.ShowZoneHint(HintZone.Notification, "<color=#a3e635>Объект перемещён!</color>", 1.5f, "tg_move", 20);
                }
                else if (state.SelectedSchematic != null)
                {
                    state.SelectedSchematic.SetTransform(hitPoint, state.SelectedSchematic.Rotation);
                    player.ShowZoneHint(HintZone.Notification, "<color=#a3e635>Схематика перемещена!</color>", 1.5f, "tg_move", 20);
                }
                break;

            case ToolGunMode.Delete:
                ExecuteDeleteAction(player, state);
                break;
        }
    }

    private static void ExecuteSecondaryAction(Player player, ToolGunState state)
    {
        // Выбор / Захват объекта лучом прицела
        if (Physics.Raycast(player.CameraTransform.position, player.CameraTransform.forward, out var hit, 40f))
        {
            var primObj = hit.collider.GetComponentInParent<PrimitiveObjectToy>();
            if (primObj != null)
            {
                state.SelectedPrimitive = Primitive.Get(primObj);
                state.SelectedSchematic = null;
                player.ShowZoneHint(HintZone.Notification, $"<color=#ffa94e>Выбран примитив: <b>{primObj.NetworkPrimitiveType}</b></color>", 2.0f, "tg_sel", 20);
                return;
            }
        }

        player.ShowZoneHint(HintZone.Notification, "<color=#ff4444>Объект не найден под прицелом.</color>", 1.5f, "tg_sel", 20);
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

                state.SelectedPrimitive = prim;
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
                state.SelectedSchematic = schem;

                player.ShowZoneHint(HintZone.Notification, $"<color=#a3e635>Заспавнена схематика <b>{name}</b>!</color>", 2.0f, "tg_spawn", 20);
                break;
            }
        }
    }

    private static void ExecuteDeleteAction(Player player, ToolGunState state)
    {
        if (state.SelectedPrimitive != null)
        {
            state.SelectedPrimitive.Destroy();
            state.SelectedPrimitive = null;
            player.ShowZoneHint(HintZone.Notification, "<color=#ff4444>Примитив удалён.</color>", 1.5f, "tg_del", 20);
            return;
        }

        if (state.SelectedSchematic != null)
        {
            SchematicLoader.RemoveInstance(state.SelectedSchematic);
            state.SelectedSchematic = null;
            player.ShowZoneHint(HintZone.Notification, "<color=#ff4444>Схематика удалена.</color>", 1.5f, "tg_del", 20);
            return;
        }

        // Попытка удалить объект напрямую под прицелом
        if (Physics.Raycast(player.CameraTransform.position, player.CameraTransform.forward, out var hit, 40f))
        {
            var primObj = hit.collider.GetComponentInParent<PrimitiveObjectToy>();
            if (primObj != null)
            {
                var prim = Primitive.Get(primObj);
                prim?.Destroy();
                player.ShowZoneHint(HintZone.Notification, "<color=#ff4444>Объект под прицелом удалён.</color>", 1.5f, "tg_del", 20);
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

    public static void NextObject(Player player)
    {
        if (!States.TryGetValue(player.Id, out var state)) return;
        state.SelectedIndex++;
    }

    public static void SetGridSnap(Player player, float snap)
    {
        if (!States.TryGetValue(player.Id, out var state)) return;
        state.GridSnap = snap;
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
               $"<size=15><color=#ffffff>Режим: <color=#a3e635><b>{state.Mode}</b></color> [F] • Категория: <color=#ffa94e><b>{state.Category}</b></color> [T]\n" +
               $"Выбрано: <color=#67e8f9><b>{selectedName}</b></color> • Сетка: <color=#fcd34d><b>{snapStr}</b></color> • Масштаб: <color=#f472b6><b>{state.CurrentScale:F1}x</b></color>\n" +
               $"<color=#c2c2c2>[ЛКМ]: Действие • [ПКМ]: Захват • [G]: Удалить</color></size>";
    }

    private static string GetCurrentSchematicName(ToolGunState state)
    {
        var list = SchematicLoader.GetAvailableSchematics();
        if (list.Count == 0) return "Нет файлов";
        return list[state.SelectedIndex % list.Count];
    }
}
