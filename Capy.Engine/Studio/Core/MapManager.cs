using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Capy.Engine.Studio.Models;
using Exiled.API.Enums;
using Exiled.API.Features;
using UnityEngine;

namespace Capy.Engine.Studio.Core;

/// <summary>
/// Менеджер карт и окружения CapyStudio.
/// Управляет привязкой схематик к комнатам, глобальными картами и событиями раунда.
/// </summary>
public static class MapManager
{
    public static string MapsDirectoryPath { get; private set; } = string.Empty;
    private static readonly List<SchematicObject> ActiveMapObjects = new();

    public static void Initialize()
    {
        MapsDirectoryPath = Path.Combine(Paths.Configs, "CapyLib", "Maps");
        try
        {
            if (!Directory.Exists(MapsDirectoryPath))
                Directory.CreateDirectory(MapsDirectoryPath);
        }
        catch { }
    }

    public static void OnRoundStarted()
    {
        // Очищаем старые карты перед новым раундом
        ClearCurrentMap();
    }

    public static void OnRoundRestarted()
    {
        ClearCurrentMap();
        SchematicLoader.DestroyAll();
    }

    /// <summary>
    /// Спавнит схематику внутри указанной комнаты с учётом локального поворота и смещения комнаты.
    /// </summary>
    public static SchematicObject? SpawnInRoom(Room room, string schematicName, Vector3? localOffset = null, Quaternion? localRotation = null, Vector3? scale = null)
    {
        if (room == null) return null;

        Vector3 offset = localOffset ?? Vector3.zero;
        Quaternion rot = localRotation ?? Quaternion.identity;

        Vector3 worldPos = room.Position + (room.Rotation * offset);
        Quaternion worldRot = room.Rotation * rot;

        return SchematicLoader.Spawn(schematicName, worldPos, worldRot, scale);
    }

    /// <summary>
    /// Загружает и строит карту по имени файла.
    /// </summary>
    public static bool LoadMap(string mapName, out string response)
    {
        string filePath = Path.Combine(MapsDirectoryPath, mapName + ".json");
        if (!File.Exists(filePath))
        {
            response = $"<color=red>[ОШИБКА]</color> Файл карты '{mapName}.json' не найден.";
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var mapData = JsonSerializer.Deserialize<MapData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (mapData == null)
            {
                response = "<color=red>[ОШИБКА]</color> Не удалось распарсить файл карты.";
                return false;
            }

            // Предотвращаем дублирование: снимаем предыдущую карту перед загрузкой новой
            int cleared = ClearCurrentMap();
            if (cleared > 0)
                Log.Info($"[CapyStudio] Перед загрузкой карты '{mapName}' выгружена предыдущая ({cleared} объектов).");

            int spawnedCount = 0;

            // 1. Спавн схематик, привязанных к типам комнат
            foreach (var entry in mapData.Entries)
            {
                var matchingRooms = Room.List.Where(r => r.Type == entry.TargetRoom).ToList();
                foreach (var room in matchingRooms)
                {
                    var spawned = SpawnInRoom(
                        room,
                        entry.SchematicName,
                        entry.LocalPosition.ToUnityVector3(),
                        entry.LocalRotation.ToUnityQuaternion(),
                        entry.Scale.ToUnityVector3()
                    );

                    if (spawned != null)
                    {
                        ActiveMapObjects.Add(spawned);
                        spawnedCount++;
                    }
                }
            }

            // 2. Спавн глобальных блоков
            if (mapData.GlobalBlocks.Count > 0)
            {
                var globalSchem = new SchematicData
                {
                    Blocks = mapData.GlobalBlocks
                };

                var spawnedGlobal = SchematicLoader.Spawn(globalSchem, mapName + "_Global", Vector3.zero, Quaternion.identity, Vector3.one);
                if (spawnedGlobal != null)
                {
                    ActiveMapObjects.Add(spawnedGlobal);
                    spawnedCount++;
                }
            }

            response = $"<color=green>[КАРТА]</color> Карта <b>{mapData.MapName}</b> успешно загружена (объектов: {spawnedCount})!";
            return true;
        }
        catch (Exception ex)
        {
            response = $"<color=red>[ОШИБКА]</color> Ошибка при загрузке карты: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Очищает все объекты текущей загруженной карты.
    /// </summary>
    public static int ClearCurrentMap()
    {
        int count = ActiveMapObjects.Count;
        foreach (var obj in ActiveMapObjects)
        {
            try { SchematicLoader.RemoveInstance(obj); } catch { }
        }
        ActiveMapObjects.Clear();
        return count;
    }

    /// <summary>
    /// Убирает уничтоженные вне MapManager'а схематики из списка объектов карты
    /// (например, при .schem clear / SchematicLoader.DestroyAll), чтобы список не протухал.
    /// </summary>
    internal static void DetachDestroyed(IEnumerable<SchematicObject> destroyed)
    {
        if (destroyed == null) return;

        var set = new HashSet<SchematicObject>(destroyed);
        ActiveMapObjects.RemoveAll(o => set.Contains(o));
    }
}
