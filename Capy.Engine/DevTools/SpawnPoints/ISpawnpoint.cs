using Exiled.API.Enums;
using UnityEngine;

namespace Capy.Engine.DevTools.SpawnPoints;

public interface ISpawnpoint {
    string Name { get; }

    RoomType RoomName { get; }

    Vector3 Offset { get; }

    Vector3 Rotation { get; }

    int Chance { get; }

    bool IsFrozen { get; }

    Vector3 GetGlobalPosition();

    Quaternion GetGlobalRotation();
}