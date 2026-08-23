using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Exiled.API.Enums;
using Exiled.API.Features;
using MapGeneration;
using Utf8Json;
using UnityEngine;

namespace Capy.Engine.DevTools.SpawnPoints;

public class SavedPointData
{
    public string Name { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public Vector3 GlobalPosition { get; set; }
    public Vector3 GlobalRotation { get; set; }
    public Vector3 LocalOffset { get; set; }
    public Vector3 LocalRotation { get; set; }
    public string CsSnippetRoom { get; set; } = string.Empty;
    public string CsSnippetGlobal { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class SpawnPointTool
{
    private static readonly List<SavedPointData> SavedPoints = new();
    private static readonly string StoragePath = Path.Combine(Paths.Configs, "AspectLib", "saved_spawnpoints.json");

    static SpawnPointTool()
    {
        LoadSavedPoints();
    }

    public static SavedPointData CaptureCurrentPosition(Player player, string pointName = "Point")
    {
        if (player == null || player.ReferenceHub == null)
            throw new ArgumentNullException(nameof(player));

        Vector3 globalPos = player.Position;
        Quaternion globalRot = player.Rotation;
        Vector3 rotEuler = globalRot.eulerAngles;

        Room? room = Room.Get(globalPos) ?? Room.List.FirstOrDefault(r => r.Zone == ZoneType.Surface);
        RoomType rName = room?.Type ?? RoomType.Surface;
        ZoneType zone = room?.Zone ?? ZoneType.Surface;

        Vector3 localOffset = Vector3.zero;
        Vector3 localRot = rotEuler;

        if (room != null && room.Transform != null)
        {
            localOffset = room.Transform.InverseTransformPoint(globalPos);
            localRot = (Quaternion.Inverse(room.Rotation) * globalRot).eulerAngles;
        }

        return CreatePointData(pointName, rName.ToString(), zone.ToString(), globalPos, rotEuler, localOffset, localRot);
    }

    public static SavedPointData CaptureRaycastPosition(Player player, string pointName = "RayPoint")
    {
        if (player == null || player.ReferenceHub == null)
            throw new ArgumentNullException(nameof(player));

        Transform camera = player.ReferenceHub.PlayerCameraReference;
        Vector3 origin = camera.position;
        Vector3 direction = camera.forward;

        Vector3 targetPos = origin + direction * 3.0f; // Дефолт 3м впереди
        Quaternion targetRot = player.Rotation;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, 100f))
        {
            targetPos = hit.point;
            targetRot = Quaternion.LookRotation(hit.normal);
        }

        Room? room = Room.Get(targetPos) ?? Room.List.FirstOrDefault(r => r.Zone == ZoneType.Surface);
        RoomType rName = room?.Type ?? RoomType.Surface;
        ZoneType zone = room?.Zone ?? ZoneType.Surface;

        Vector3 localOffset = Vector3.zero;
        Vector3 localRot = targetRot.eulerAngles;

        if (room != null && room.Transform != null)
        {
            localOffset = room.Transform.InverseTransformPoint(targetPos);
            localRot = (Quaternion.Inverse(room.Rotation) * targetRot).eulerAngles;
        }

        return CreatePointData(pointName, rName.ToString(), zone.ToString(), targetPos, targetRot.eulerAngles, localOffset, localRot);
    }

    private static SavedPointData CreatePointData(string name, string roomName, string zone, Vector3 globalPos, Vector3 globalRot, Vector3 localOffset, Vector3 localRot)
    {
        string csRoom = $"new RoomSpawnPoint(\"{name}\", RoomName.{roomName}, new Vector3({FormatF(localOffset.x)}, {FormatF(localOffset.y)}, {FormatF(localOffset.z)}), new Vector3({FormatF(localRot.x)}, {FormatF(localRot.y)}, {FormatF(localRot.z)}), 100, false)";
        string csGlobal = $"new Vector3({FormatF(globalPos.x)}, {FormatF(globalPos.y)}, {FormatF(globalPos.z)})";

        var data = new SavedPointData
        {
            Name = name,
            RoomName = roomName,
            Zone = zone,
            GlobalPosition = globalPos,
            GlobalRotation = globalRot,
            LocalOffset = localOffset,
            LocalRotation = localRot,
            CsSnippetRoom = csRoom,
            CsSnippetGlobal = csGlobal,
            CreatedAt = DateTime.UtcNow
        };

        return data;
    }

    public static void SavePoint(SavedPointData data)
    {
        if (data == null) return;

        SavedPoints.Add(data);
        SaveToFile();

        Log.Info($"[SpawnPointTool] Сохранена точка '{data.Name}'! C# Код:\n{data.CsSnippetRoom}");
    }

    public static IReadOnlyList<SavedPointData> GetSavedPoints() => SavedPoints.AsReadOnly();

    public static void ClearSavedPoints()
    {
        SavedPoints.Clear();
        SaveToFile();
    }

    private static string FormatF(float val) => $"{val:F2}f".Replace(',', '.');

    private static void SaveToFile()
    {
        try
        {
            string dir = Path.GetDirectoryName(StoragePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string json = JsonSerializer.PrettyPrint(JsonSerializer.ToJsonString(SavedPoints));
            File.WriteAllText(StoragePath, json);
        }
        catch (Exception ex)
        {
            Log.Error($"[SpawnPointTool] Ошибка сохранения точек в файл: {ex}");
        }
    }

    private static void LoadSavedPoints()
    {
        try
        {
            if (File.Exists(StoragePath))
            {
                string json = File.ReadAllText(StoragePath);
                var loaded = JsonSerializer.Deserialize<List<SavedPointData>>(json);
                if (loaded != null)
                {
                    SavedPoints.Clear();
                    SavedPoints.AddRange(loaded);
                }
            }
        }
        catch { }
    }
}
