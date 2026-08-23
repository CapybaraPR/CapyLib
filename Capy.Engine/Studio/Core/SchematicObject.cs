using System;
using System.Collections.Generic;
using AdminToys;
using Capy.Engine.Studio.Models;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Toys;
using UnityEngine;
using Light = Exiled.API.Features.Toys.Light;

namespace Capy.Engine.Studio.Core;

/// <summary>
/// Представляет живой экземпляр заспавненной схематики в игровом мире.
/// Управляет всеми примитивами, источниками света, дверями, игрушками и объектами, входящими в схематику.
/// </summary>
public sealed class SchematicObject
{
    public string Name { get; set; } = "Schematic";
    public SchematicData Data { get; }
    public Vector3 Position { get; private set; }
    public Quaternion Rotation { get; private set; }
    public Vector3 Scale { get; private set; } = Vector3.one;
    public bool IsDestroyed { get; private set; }

    public GameObject? RootObject { get; set; }

    public List<Primitive> SpawnedPrimitives { get; } = new();
    public List<Light> SpawnedLights { get; } = new();
    public List<Door> SpawnedDoors { get; } = new();
    public List<GameObject> SpawnedGameObjects { get; } = new();

    private readonly List<(BlockData Definition, Primitive Primitive)> _primitivePairs = new();
    private readonly List<(BlockData Definition, Light Light)> _lightPairs = new();
    private readonly List<(BlockData Definition, GameObject GameObject)> _gameObjectPairs = new();

    public SchematicObject(string name, SchematicData data, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Name = name;
        Data = data;
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    internal void AddPrimitive(BlockData def, Primitive primitive)
    {
        SpawnedPrimitives.Add(primitive);
        _primitivePairs.Add((def, primitive));
    }

    internal void AddLight(BlockData def, Light light)
    {
        SpawnedLights.Add(light);
        _lightPairs.Add((def, light));
    }

    internal void AddDoor(Door door)
    {
        SpawnedDoors.Add(door);
    }

    internal void AddGameObject(BlockData def, GameObject go)
    {
        SpawnedGameObjects.Add(go);
        _gameObjectPairs.Add((def, go));
    }

    /// <summary>
    /// Перемещает и поворачивает всю схематику целиком с пересчетом координат всех вложенных блоков.
    /// </summary>
    public void SetTransform(Vector3 newPosition, Quaternion newRotation, Vector3? newScale = null)
    {
        if (IsDestroyed) return;

        Position = newPosition;
        Rotation = newRotation;
        if (newScale.HasValue)
            Scale = newScale.Value;

        if (RootObject != null)
        {
            RootObject.transform.position = Position;
            RootObject.transform.rotation = Rotation;
            RootObject.transform.localScale = Scale;
        }

        UpdateChildTransforms();
    }

    /// <summary>
    /// Обновляет позиции всех примитивов, источников света и объектов относительно текущего центра схематики.
    /// </summary>
    public void UpdateChildTransforms()
    {
        if (IsDestroyed) return;

        // 1. Обновляем примитивы
        foreach (var (def, prim) in _primitivePairs)
        {
            if (prim == null || prim.Base == null) continue;

            Vector3 localPos = def.Position.ToUnityVector3();
            Vector3 scaledLocalPos = Vector3.Scale(localPos, Scale);
            Vector3 worldPos = Position + (Rotation * scaledLocalPos);

            Quaternion localRot = def.Rotation.ToUnityQuaternion();
            Quaternion worldRot = Rotation * localRot;

            Vector3 worldScale = Vector3.Scale(def.Scale.ToUnityVector3(), Scale);

            prim.Position = worldPos;
            prim.Rotation = worldRot;
            prim.Scale = worldScale;
        }

        // 2. Обновляем источники света
        foreach (var (def, light) in _lightPairs)
        {
            if (light == null || light.Base == null) continue;

            Vector3 localPos = def.Position.ToUnityVector3();
            Vector3 scaledLocalPos = Vector3.Scale(localPos, Scale);
            Vector3 worldPos = Position + (Rotation * scaledLocalPos);

            light.Position = worldPos;
            light.Rotation = Rotation * def.Rotation.ToUnityQuaternion();
        }

        // 3. Обновляем кастомные GameObjects (двери, верстаки, надписи, телепорты)
        foreach (var (def, go) in _gameObjectPairs)
        {
            if (go == null) continue;

            Vector3 localPos = def.Position.ToUnityVector3();
            Vector3 scaledLocalPos = Vector3.Scale(localPos, Scale);
            Vector3 worldPos = Position + (Rotation * scaledLocalPos);

            Quaternion localRot = def.Rotation.ToUnityQuaternion();
            Quaternion worldRot = Rotation * localRot;

            Vector3 worldScale = Vector3.Scale(def.Scale.ToUnityVector3(), Scale);

            go.transform.position = worldPos;
            go.transform.rotation = worldRot;
            go.transform.localScale = worldScale;
        }
    }

    /// <summary>
    /// Полностью удаляет схематику и все её объекты из игрового мира.
    /// </summary>
    public void Destroy()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;

        SchematicAnimationController.Unregister(this);

        foreach (var prim in SpawnedPrimitives)
        {
            try { prim?.Destroy(); } catch { }
        }

        foreach (var light in SpawnedLights)
        {
            try { light?.Destroy(); } catch { }
        }

        foreach (var door in SpawnedDoors)
        {
            try
            {
                if (door?.Base != null)
                    UnityEngine.Object.Destroy(door.Base.gameObject);
            }
            catch { }
        }

        foreach (var go in SpawnedGameObjects)
        {
            try
            {
                if (go != null)
                    UnityEngine.Object.Destroy(go);
            }
            catch { }
        }

        if (RootObject != null)
        {
            try { UnityEngine.Object.Destroy(RootObject); } catch { }
        }

        SpawnedPrimitives.Clear();
        SpawnedLights.Clear();
        SpawnedDoors.Clear();
        SpawnedGameObjects.Clear();
        _primitivePairs.Clear();
        _lightPairs.Clear();
        _gameObjectPairs.Clear();
    }
}
