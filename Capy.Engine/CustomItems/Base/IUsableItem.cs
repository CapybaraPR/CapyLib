using UnityEngine;

namespace Capy.Engine.CustomItems.Base;

public interface ISpawnableItem
{
    float SpawnChance { get; }
}

public interface IGlowingItem
{
    Color GlowColor { get; }
    float GlowIntensity { get; }
    float GlowRange { get; }
    bool GlowInHands { get; }
}

public interface IUsableItem
{
    float Cooldown { get; }
    void OnUsed(Player player);
}
