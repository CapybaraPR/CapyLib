using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AdminToys;
using Capy.Engine.Studio.Models;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Toys;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;
using Light = Exiled.API.Features.Toys.Light;

namespace Capy.Engine.Studio.Core;

/// <summary>
/// Главный загрузчик и парсер схематик CapyStudio.
/// Обеспечивает нативный спавн примитивов, источников света, дверей, телепортов, текста и локеров.
/// </summary>
public static class SchematicLoader
{
    private static readonly ConcurrentDictionary<string, (SchematicData Data, DateTime LastModified)> CachedSchematics = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<SchematicObject> ActiveInstances = new();

    private static List<string>? _availableListCache;
    private static DateTime _availableListCacheTime = DateTime.MinValue;
    private const float AvailableListCacheSeconds = 10f;

    public static string PrimarySchematicsPath { get; private set; } = string.Empty;
    public static string FallbackSchematicsPath { get; private set; } = string.Empty;

    /// <summary>
    /// Убирает из имени схематики символы путей — защита от path traversal (.schem spawn ..\..\file).
    /// </summary>
    public static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return name.Replace("..", "_").Trim();
    }

    public static void ClearCache()
    {
        CachedSchematics.Clear();
        InvalidateAvailableListCache();
    }

    internal static void InvalidateAvailableListCache()
    {
        _availableListCache = null;
    }

    public static IReadOnlyList<SchematicObject> SpawnedSchematics
    {
        get
        {
            lock (ActiveInstances)
                return ActiveInstances.ToArray();
        }
    }

    public static void Initialize()
    {
        string exiledConfigPath = Path.Combine(Paths.Configs, "CapyLib", "Schematics");
        string merConfigPath = Path.Combine(Paths.Configs, "MapEditorReborn", "Schematics");

        PrimarySchematicsPath = exiledConfigPath;
        FallbackSchematicsPath = merConfigPath;

        try
        {
            if (!Directory.Exists(PrimarySchematicsPath))
                Directory.CreateDirectory(PrimarySchematicsPath);

            PrefabManager.Initialize();
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyStudio] Ошибка при создании папки схематик: {ex}");
        }

        Log.Info($"[CapyStudio] Движок схематик инициализирован. Путь: {PrimarySchematicsPath}");
    }

    public static List<string> GetAvailableSchematics()
    {
        if (_availableListCache != null && (DateTime.UtcNow - _availableListCacheTime).TotalSeconds < AvailableListCacheSeconds)
            return new List<string>(_availableListCache);

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void ScanDir(string dir)
        {
            if (!Directory.Exists(dir)) return;

            foreach (var file in Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
            {
                result.Add(Path.GetFileNameWithoutExtension(file));
            }

            foreach (var subDir in Directory.GetDirectories(dir))
            {
                string dirName = Path.GetFileName(subDir);
                string expectedJson = Path.Combine(subDir, dirName + ".json");
                if (File.Exists(expectedJson))
                    result.Add(dirName);
            }
        }

        ScanDir(PrimarySchematicsPath);
        ScanDir(FallbackSchematicsPath);

        _availableListCache = new List<string>(result);
        _availableListCacheTime = DateTime.UtcNow;
        return new List<string>(result);
    }

    public static string? FindSchematicFile(string schematicName)
    {
        schematicName = SanitizeName(schematicName);
        if (string.IsNullOrWhiteSpace(schematicName))
            return null;

        string[] searchDirs = { PrimarySchematicsPath, FallbackSchematicsPath };

        foreach (var dir in searchDirs)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                continue;

            string directFile = Path.Combine(dir, schematicName + ".json");
            if (File.Exists(directFile))
                return directFile;

            string nestedFile = Path.Combine(dir, schematicName, schematicName + ".json");
            if (File.Exists(nestedFile))
                return nestedFile;
        }

        return null;
    }

    public static SchematicData? LoadSchematicData(string schematicName, bool forceReload = false)
    {
        string? filePath = FindSchematicFile(schematicName);
        if (filePath == null)
        {
            Log.Warn($"[CapyStudio] Схематика '{schematicName}' не найдена.");
            return null;
        }

        DateTime fileModified = File.GetLastWriteTimeUtc(filePath);

        if (!forceReload && CachedSchematics.TryGetValue(schematicName, out var cached))
        {
            if (cached.LastModified >= fileModified)
                return cached.Data;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var data = JsonSerializer.Deserialize<SchematicData>(json, options);
            if (data != null)
            {
                CachedSchematics[schematicName] = (data, fileModified);
                return data;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyStudio] Ошибка чтения файла схематики '{filePath}': {ex}");
        }

        return null;
    }

    /// <summary>
    /// Спавнит схематику в мире по имени и координатам.
    /// </summary>
    public static SchematicObject? Spawn(string schematicName, Vector3 position, Quaternion? rotation = null, Vector3? scale = null)
    {
        var data = LoadSchematicData(schematicName);
        if (data == null)
            return null;

        return Spawn(data, schematicName, position, rotation, scale);
    }

    /// <summary>
    /// Спавнит схематику на основе готовой модели данных.
    /// </summary>
    public static SchematicObject? Spawn(SchematicData data, string name, Vector3 position, Quaternion? rotation = null, Vector3? scale = null)
    {
        PrefabManager.Initialize();

        Quaternion rootRot = rotation ?? Quaternion.identity;
        Vector3 rootScale = scale ?? Vector3.one;

        var schemObj = new SchematicObject(name, data, position, rootRot, rootScale);

        try
        {
            foreach (var block in data.Blocks)
            {
                Vector3 localPos = block.Position.ToUnityVector3();
                Vector3 scaledLocalPos = Vector3.Scale(localPos, rootScale);
                Vector3 worldPos = position + (rootRot * scaledLocalPos);

                Quaternion localRot = block.Rotation.ToUnityQuaternion();
                Quaternion worldRot = rootRot * localRot;

                Vector3 worldScale = Vector3.Scale(block.Scale.ToUnityVector3(), rootScale);

                switch ((BlockType)block.BlockType)
                {
                    case BlockType.Primitive:
                    {
                        var flags = block.GetPrimitiveFlags();
                        var prim = Primitive.Create(
                            primitiveType: block.GetPrimitiveType(),
                            flags: flags,
                            position: worldPos,
                            rotation: worldRot.eulerAngles,
                            scale: worldScale,
                            spawn: true,
                            color: block.GetColor()
                        );

                        if (prim != null)
                        {
                            bool isCollidable = (flags & PrimitiveFlags.Collidable) != 0;
                            if (!isCollidable && prim.GameObject != null)
                            {
                                try
                                {
                                    foreach (var col in prim.GameObject.GetComponentsInChildren<Collider>())
                                    {
                                        col.enabled = false;
                                    }
                                }
                                catch { }
                            }

                            schemObj.AddPrimitive(block, prim);
                        }
                        break;
                    }

                    case BlockType.LightSource:
                    {
                        var light = Light.Create(
                            position: worldPos,
                            rotation: worldRot.eulerAngles,
                            scale: Vector3.one,
                            spawn: false,
                            color: block.GetColor()
                        );

                        if (light != null)
                        {
                            light.Intensity = block.GetLightIntensity();
                            light.Range = block.GetLightRange();
                            light.Spawn();
                            schemObj.AddLight(block, light);
                        }
                        break;
                    }

                    case BlockType.Teleport:
                    {
                        // Создаём функциональный телепорт
                        var tpGo = new GameObject($"Teleport_{block.Name}");
                        tpGo.transform.position = worldPos;
                        tpGo.transform.rotation = worldRot;
                        tpGo.transform.localScale = worldScale;

                        var col = tpGo.AddComponent<BoxCollider>();
                        col.isTrigger = true;
                        col.size = Vector3.one * 1.5f;

                        var tpComp = tpGo.AddComponent<TeleportComponent>();
                        tpComp.Cooldown = block.GetTeleportCooldown();
                        tpComp.TeleportId = block.GetTeleportId();
                        tpComp.Targets = block.GetTeleportTargets();

                        // Визуальный полупрозрачный куб-индикатор
                        var visualPrim = Primitive.Create(
                            primitiveType: PrimitiveType.Cube,
                            flags: PrimitiveFlags.Visible,
                            position: worldPos,
                            rotation: worldRot.eulerAngles,
                            scale: worldScale,
                            spawn: true,
                            color: new Color(0.2f, 0.6f, 1.0f, 0.4f)
                        );

                        schemObj.AddGameObject(block, tpGo);
                        if (visualPrim != null) schemObj.AddPrimitive(block, visualPrim);
                        break;
                    }

                    case BlockType.Capybara:
                    {
                        var capy = Exiled.API.Features.Toys.Capybara.Create(position: worldPos, rotation: worldRot, scale: worldScale, collidable: true, spawn: true);
                        if (capy?.Base != null)
                            schemObj.AddGameObject(block, capy.Base.gameObject);
                        break;
                    }

                    case BlockType.Text:
                    {
                        var textToy = Exiled.API.Features.Toys.Text.Create(
                            position: worldPos,
                            rotation: worldRot,
                            scale: worldScale,
                            text: block.GetTextContent(),
                            spawn: true
                        );
                        if (textToy?.Base != null)
                            schemObj.AddGameObject(block, textToy.Base.gameObject);
                        break;
                    }

                    case BlockType.Door:
                    {
                        var doorPrefab = block.GetDoorType().ToLowerInvariant() switch
                        {
                            "hcz" => PrefabManager.DoorHcz,
                            "ez" => PrefabManager.DoorEz,
                            "bulk" => PrefabManager.DoorHeavyBulk,
                            "gate" => PrefabManager.DoorGate,
                            _ => PrefabManager.DoorLcz
                        };

                        if (doorPrefab != null)
                        {
                            // Двери не масштабируем — scale ломает их коллизии и анимации
                            var doorGo = UnityEngine.Object.Instantiate(doorPrefab.gameObject, worldPos, worldRot);
                            NetworkServer.Spawn(doorGo);
                            schemObj.AddGameObject(block, doorGo);

                            var variant = doorGo.GetComponent<DoorVariant>();
                            if (variant != null)
                            {
                                var exiledDoor = Door.Get(variant);
                                if (exiledDoor != null)
                                    schemObj.AddDoor(exiledDoor);
                            }
                        }
                        else
                        {
                            Log.Warn($"[CapyStudio] Дверь '{block.GetDoorType()}' в схематике '{name}' пропущена: префаб недоступен (префабы ещё не загружены?).");
                        }
                        break;
                    }

                    case BlockType.Workstation:
                    {
                        if (PrefabManager.WorkstationPrefab != null)
                        {
                            var wsGo = UnityEngine.Object.Instantiate(PrefabManager.WorkstationPrefab.gameObject, worldPos, worldRot);
                            wsGo.transform.localScale = worldScale;
                            NetworkServer.Spawn(wsGo);
                            schemObj.AddGameObject(block, wsGo);
                        }
                        break;
                    }

                    case BlockType.ShootingTarget:
                    {
                        var targetPrefab = block.GetShootingTargetType().ToLowerInvariant() switch
                        {
                            "dboy" => PrefabManager.TargetDBoy,
                            "binary" => PrefabManager.TargetBinary,
                            _ => PrefabManager.TargetSport
                        };

                        if (targetPrefab != null)
                        {
                            var targetGo = UnityEngine.Object.Instantiate(targetPrefab.gameObject, worldPos, worldRot);
                            targetGo.transform.localScale = worldScale;
                            NetworkServer.Spawn(targetGo);
                            schemObj.AddGameObject(block, targetGo);
                        }
                        break;
                    }

                    case BlockType.Locker:
                    {
                        var lockerPrefab = block.GetLockerType().ToLowerInvariant() switch
                        {
                            string s when s.Contains("207") => PrefabManager.Pedestal207,
                            string s when s.Contains("018") => PrefabManager.Pedestal018,
                            string s when s.Contains("gun") => PrefabManager.LockerLargeGun,
                            string s when s.Contains("medkit") => PrefabManager.LockerMedkit,
                            string s when s.Contains("rifle") => PrefabManager.LockerRifleRack,
                            _ => PrefabManager.Pedestal500
                        };

                        if (lockerPrefab != null)
                        {
                            var lockerGo = UnityEngine.Object.Instantiate(lockerPrefab.gameObject, worldPos, worldRot);
                            lockerGo.transform.localScale = worldScale;
                            NetworkServer.Spawn(lockerGo);
                            schemObj.AddGameObject(block, lockerGo);
                        }
                        break;
                    }
                }
            }

            lock (ActiveInstances)
            {
                ActiveInstances.Add(schemObj);
            }

            return schemObj;
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyStudio] Ошибка при спавне блоков схематики '{name}': {ex}");
            schemObj.Destroy();
            return null;
        }
    }

    /// <summary>
    /// Сохраняет схематику в файл .json.
    /// </summary>
    public static bool Save(SchematicData data, string name, out string path)
    {
        name = SanitizeName(name);
        path = Path.Combine(PrimarySchematicsPath, name + ".json");
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(path, json);
            CachedSchematics[name] = (data, DateTime.UtcNow);
            InvalidateAvailableListCache();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyStudio] Ошибка сохранения схематики '{name}': {ex}");
            return false;
        }
    }

    /// <summary>
    /// Объединяет несколько схематик в одну новую.
    /// </summary>
    public static bool Merge(string name1, string name2, string outputName, out string path)
    {
        path = string.Empty;
        var s1 = LoadSchematicData(name1);
        var s2 = LoadSchematicData(name2);

        if (s1 == null || s2 == null) return false;

        var merged = new SchematicData
        {
            RootObjectId = 10000,
            Blocks = new List<BlockData>()
        };

        int maxId = 10000;

        // Глубокие копии: иначе merged шарит экземпляры BlockData с закэшированной схематикой
        foreach (var b in s1.Blocks)
        {
            maxId = Math.Max(maxId, b.ObjectId);
            merged.Blocks.Add(new BlockData
            {
                Name = b.Name,
                ObjectId = b.ObjectId,
                ParentId = b.ParentId,
                Position = b.Position,
                Rotation = b.Rotation,
                Scale = b.Scale,
                BlockType = b.BlockType,
                Properties = new Dictionary<string, object>(b.Properties)
            });
        }

        foreach (var b in s2.Blocks)
        {
            maxId++;
            var cloned = new BlockData
            {
                Name = b.Name,
                ObjectId = maxId,
                ParentId = 10000,
                Position = b.Position,
                Rotation = b.Rotation,
                Scale = b.Scale,
                BlockType = b.BlockType,
                Properties = new Dictionary<string, object>(b.Properties)
            };
            merged.Blocks.Add(cloned);
        }

        return Save(merged, outputName, out path);
    }

    /// <summary>
    /// Удаляет схематику из списка активных.
    /// </summary>
    public static void RemoveInstance(SchematicObject obj)
    {
        lock (ActiveInstances)
        {
            ActiveInstances.Remove(obj);
        }
        obj.Destroy();
    }

    /// <summary>
    /// Удаляет все заспавненные схематики и очищает память.
    /// </summary>
    public static void DestroyAll()
    {
        SchematicObject[] snapshot;
        lock (ActiveInstances)
        {
            snapshot = ActiveInstances.ToArray();
        }

        // Синхронизируем MapManager, чтобы в ActiveMapObjects не осталось протухших ссылок
        Capy.Engine.Studio.Core.MapManager.DetachDestroyed(snapshot);

        lock (ActiveInstances)
        {
            foreach (var instance in snapshot)
            {
                instance.Destroy();
            }
            ActiveInstances.Clear();
        }
    }
}
