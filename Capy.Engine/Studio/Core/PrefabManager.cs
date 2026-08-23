using System;
using System.Collections.Generic;
using AdminToys;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Firearms.Attachments;
using MapGeneration.Distributors;
using Mirror;
using UnityEngine;
using LightSourceToy = AdminToys.LightSourceToy;
using PrimitiveObjectToy = AdminToys.PrimitiveObjectToy;

namespace Capy.Engine.Studio.Core;

/// <summary>
/// Менеджер сетевых префабов SCP:SL для нативного спавна всех типов объектов в CapyStudio.
/// </summary>
public static class PrefabManager
{
    public static PrimitiveObjectToy? PrimitivePrefab { get; private set; }
    public static LightSourceToy? LightPrefab { get; private set; }
    public static CapybaraToy? CapybaraPrefab { get; private set; }
    public static TextToy? TextPrefab { get; private set; }
    public static InvisibleInteractableToy? InteractablePrefab { get; private set; }
    public static WorkstationController? WorkstationPrefab { get; private set; }

    // Двери
    public static DoorVariant? DoorLcz { get; private set; }
    public static DoorVariant? DoorHcz { get; private set; }
    public static DoorVariant? DoorEz { get; private set; }
    public static DoorVariant? DoorHeavyBulk { get; private set; }
    public static DoorVariant? DoorGate { get; private set; }

    // Мишени
    public static ShootingTarget? TargetSport { get; private set; }
    public static ShootingTarget? TargetDBoy { get; private set; }
    public static ShootingTarget? TargetBinary { get; private set; }

    // Камеры 079
    public static Scp079CameraToy? CameraLcz { get; private set; }
    public static Scp079CameraToy? CameraHcz { get; private set; }

    // Шкафчики и постаменты
    public static Locker? Pedestal500 { get; private set; }
    public static Locker? Pedestal207 { get; private set; }
    public static Locker? Pedestal018 { get; private set; }
    public static Locker? LockerLargeGun { get; private set; }
    public static Locker? LockerMedkit { get; private set; }
    public static Locker? LockerRifleRack { get; private set; }

    public static bool IsInitialized { get; private set; }

    private static DateTime _lastInitWarnTime = DateTime.MinValue;

    public static void Initialize()
    {
        if (IsInitialized) return;

        try
        {
            int foundCount = 0;

            foreach (GameObject gameObject in NetworkClient.prefabs.Values)
            {
                if (gameObject == null) continue;

                if (PrimitivePrefab == null && gameObject.TryGetComponent(out PrimitiveObjectToy prim))
                {
                    PrimitivePrefab = prim;
                    foundCount++;
                    continue;
                }

                if (LightPrefab == null && gameObject.TryGetComponent(out LightSourceToy light))
                {
                    LightPrefab = light;
                    foundCount++;
                    continue;
                }

                if (CapybaraPrefab == null && gameObject.TryGetComponent(out CapybaraToy capy))
                {
                    CapybaraPrefab = capy;
                    foundCount++;
                    continue;
                }

                if (TextPrefab == null && gameObject.TryGetComponent(out TextToy text))
                {
                    TextPrefab = text;
                    foundCount++;
                    continue;
                }

                if (InteractablePrefab == null && gameObject.TryGetComponent(out InvisibleInteractableToy interact))
                {
                    InteractablePrefab = interact;
                    foundCount++;
                    continue;
                }

                if (WorkstationPrefab == null && gameObject.TryGetComponent(out WorkstationController ws))
                {
                    WorkstationPrefab = ws;
                    foundCount++;
                    continue;
                }

                if (gameObject.TryGetComponent(out DoorVariant door))
                {
                    string name = gameObject.name;
                    // Порядок важен: "EZ Gate" содержит и "EZ", и "Gate" — Gate проверяем раньше
                    if (name.Contains("LCZ")) DoorLcz = door;
                    else if (name.Contains("HCZ") && !name.Contains("Bulk")) DoorHcz = door;
                    else if (name.Contains("Bulk")) DoorHeavyBulk = door;
                    else if (name.Contains("Gate")) DoorGate = door;
                    else if (name.Contains("EZ")) DoorEz = door;

                    foundCount++;
                    continue;
                }

                if (gameObject.TryGetComponent(out ShootingTarget target))
                {
                    string name = gameObject.name.ToLowerInvariant();
                    if (name.Contains("sport")) TargetSport = target;
                    else if (name.Contains("dboy")) TargetDBoy = target;
                    else if (name.Contains("binary")) TargetBinary = target;

                    foundCount++;
                    continue;
                }

                if (gameObject.TryGetComponent(out Scp079CameraToy cam))
                {
                    if (gameObject.name.Contains("Lcz")) CameraLcz = cam;
                    else CameraHcz = cam;

                    foundCount++;
                    continue;
                }

                if (gameObject.TryGetComponent(out Locker locker))
                {
                    string name = gameObject.name;
                    if (name.Contains("500")) Pedestal500 = locker;
                    else if (name.Contains("207")) Pedestal207 = locker;
                    else if (name.Contains("018")) Pedestal018 = locker;
                    else if (name.Contains("LargeGun")) LockerLargeGun = locker;
                    else if (name.Contains("Medkit") || name.Contains("Regular")) LockerMedkit = locker;
                    else if (name.Contains("Rifle")) LockerRifleRack = locker;

                    foundCount++;
                    continue;
                }
            }

            // Префабы регистрируются в Mirror уже после старта плагина: если при инициализации
            // ничего не нашлось — НЕ ставим флаг навсегда, чтобы повторить попытку при следующем Spawn.
            if (foundCount > 0)
            {
                IsInitialized = true;
                Exiled.API.Features.Log.Debug($"[CapyStudio Prefabs] Инициализировано префабов: {foundCount}");
            }
            else if ((DateTime.UtcNow - _lastInitWarnTime).TotalSeconds > 60)
            {
                _lastInitWarnTime = DateTime.UtcNow;
                Exiled.API.Features.Log.Warn("[CapyStudio Prefabs] Сетевые префабы ещё не зарегистрированы (NetworkClient.prefabs пуст). Повторная попытка будет выполнена при следующем спавне.");
            }
        }
        catch (Exception ex)
        {
            Exiled.API.Features.Log.Error($"[CapyStudio Prefabs] Ошибка инициализации сетевых префабов: {ex}");
        }
    }
}
