using Capy.Engine.CustomItems.Base;
using Capy.Engine.CustomItems.Models;
using UnityEngine;

namespace Capy.Engine.CustomItems.Manager;

/// <summary>
/// Менеджер спавна кастомных предметов на карте в начале раунда.
/// </summary>
public static class SpawnManager
{
    private static readonly Dictionary<CustomItem, List<RoomSpawnPoint>> SpawnPoints = new();

    public static void RegisterSpawnPoints(CustomItem item, IEnumerable<RoomSpawnPoint> points)
    {
        if (item == null || points == null) return;

        if (!SpawnPoints.TryGetValue(item, out var list))
        {
            list = new List<RoomSpawnPoint>();
            SpawnPoints[item] = list;
        }
        list.AddRange(points);
    }

    public static void SpawnAll()
    {
        var random = new System.Random();

        foreach (var (item, points) in SpawnPoints)
        {
            foreach (var point in points)
            {
                if (random.NextDouble() * 100f <= point.Chance)
                {
                    var room = Room.Get(point.RoomType);
                    if (room != null)
                    {
                        Vector3 worldPos = room.Transform.TransformPoint(point.Offset);
                        Quaternion worldRot = room.Transform.rotation * Quaternion.Euler(point.Rotation);

                        item.Spawn(worldPos, worldRot);
                    }
                }
            }
        }
    }

    public static void Clear()
    {
        SpawnPoints.Clear();
    }
}
