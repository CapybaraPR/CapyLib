using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using UnityEngine;

namespace Capy.Engine.DevTools.SpawnPoints;

public class RoomSpawnPoint(string debugName, RoomType roomType, Vector3 offset, Vector3 rotation, int chance, bool isFrozen) : ISpawnpoint {
    public RoomType RoomName => roomType;
    public Vector3 Offset => offset;
    public Vector3 Rotation => rotation;
    public int Chance => chance;
    public string Name => debugName;
    public bool IsFrozen => isFrozen;

    public Vector3 GetGlobalPosition() {
        Room? room = Room.List.FirstOrDefault(r => r.Type == this.RoomName) ?? Room.Get(this.RoomName);
        if (room == null) {
            Log.Warn($"[SpawnPoints] Комната {this.RoomName} не найдена в текущей генерации карты для точки {Name}!");
            return Vector3.zero;
        }

        return room.Transform.TransformPoint(Offset);
    }

    public Quaternion GetGlobalRotation() {
        Room? room = Room.List.FirstOrDefault(r => r.Type == this.RoomName) ?? Room.Get(this.RoomName);
        if (room == null)
            return Quaternion.identity;

        return room.Rotation * Quaternion.Euler(this.Rotation);
    }
}
