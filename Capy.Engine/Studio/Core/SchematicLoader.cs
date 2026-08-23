using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AdminToys;
using Capy.Engine.Studio.Models;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.API.Features.Toys;
using UnityEngine;
using Light = Exiled.API.Features.Toys.Light;

namespace Capy.Engine.Studio.Core;

/// <summary>
/// Главный загрузчик и спавнер схематик движка CapyStudio.
/// Полностью автономен, совместим со схематиками MapEditorReborn (.json) и использует нативные EXILED Toys.
/// </summary>
public static class SchematicLoader
{
    private static readonly ConcurrentDictionary<string, SchematicData> CachedSchematics = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<SchematicObject> ActiveInstances = new();

    public static string PrimarySchematicsPath { get; private set; } = string.Empty;
    public static string FallbackSchematicsPath { get; private set; } = string.Empty;

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
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyStudio] Ошибка при создании папки схематик: {ex}");
        }

        Log.Info($"[CapyStudio] Движок схематик инициализирован. Путь: {PrimarySchematicsPath}");
    }

    public static List<string> GetAvailableSchematics()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void ScanDir(string dir)
        {
            if (!Directory.Exists(dir)) return;

            // 1. Прямые .json файлы
            foreach (var file in Directory.GetFiles(dir, "*.json", SearchOption.TopDirectoryOnly))
            {
                result.Add(Path.GetFileNameWithoutExtension(file));
            }

            // 2. Вложенные папки с одноименными .json (формат MER: Schematics/Capybara/Capybara.json)
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

        return new List<string>(result);
    }

    public static string? FindSchematicFile(string schematicName)
    {
        string[] searchDirs = { PrimarySchematicsPath, FallbackSchematicsPath };

        foreach (var dir in searchDirs)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                continue;

            // Вариант 1: Schematics/Name.json
            string directFile = Path.Combine(dir, schematicName + ".json");
            if (File.Exists(directFile))
                return directFile;

            // Вариант 2: Schematics/Name/Name.json
            string nestedFile = Path.Combine(dir, schematicName, schematicName + ".json");
            if (File.Exists(nestedFile))
                return nestedFile;
        }

        return null;
    }

    public static SchematicData? LoadSchematicData(string schematicName)
    {
        if (CachedSchematics.TryGetValue(schematicName, out var cached))
            return cached;

        string? filePath = FindSchematicFile(schematicName);
        if (filePath == null)
        {
            Log.Warn($"[CapyStudio] Схематика '{schematicName}' не найдена.");
            return null;
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
                CachedSchematics[schematicName] = data;
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

                switch (block.BlockType)
                {
                    case 1: // Primitive
                    {
                        var prim = Primitive.Create(
                            primitiveType: block.GetPrimitiveType(),
                            flags: block.GetPrimitiveFlags(),
                            position: worldPos,
                            rotation: worldRot.eulerAngles,
                            scale: worldScale,
                            spawn: true,
                            color: block.GetColor()
                        );

                        if (prim != null)
                            schemObj.AddPrimitive(block, prim);
                        break;
                    }

                    case 2: // LightSource
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
        path = Path.Combine(PrimarySchematicsPath, name + ".json");
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(path, json);
            CachedSchematics[name] = data;
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyStudio] Ошибка сохранения схематики '{name}': {ex}");
            return false;
        }
    }

    /// <summary>
    /// Удаляет все заспавненные схематики и очищает память.
    /// </summary>
    public static void DestroyAll()
    {
        lock (ActiveInstances)
        {
            foreach (var inst in ActiveInstances)
            {
                try { inst.Destroy(); } catch { }
            }
            ActiveInstances.Clear();
        }
    }

    public static void RemoveInstance(SchematicObject obj)
    {
        if (obj == null) return;
        lock (ActiveInstances)
        {
            ActiveInstances.Remove(obj);
        }
        obj.Destroy();
    }
}
