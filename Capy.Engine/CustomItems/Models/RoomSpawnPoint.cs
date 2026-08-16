using Exiled.API.Enums;
using UnityEngine;

namespace Capy.Engine.CustomItems.Models;

/// <summary>
/// Точка спавна предмета, привязанная к типу комнаты на карте.
/// </summary>
public class RoomSpawnPoint
{
    public RoomType RoomType { get; set; }
    public Vector3 Offset { get; set; }
    public Vector3 Rotation { get; set; }
    public float Chance { get; set; } = 100f;

    public RoomSpawnPoint() { }

    public RoomSpawnPoint(RoomType roomType, Vector3 offset, Vector3 rotation, float chance = 100f)
    {
        RoomType = roomType;
        Offset = offset;
        Rotation = rotation;
        Chance = chance;
    }
}
