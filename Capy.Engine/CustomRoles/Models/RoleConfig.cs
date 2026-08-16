using PlayerRoles;

namespace Capy.Engine.CustomRoles.Models;

public class RoleConfig
{
    public float MaxHealth { get; set; } = 100f;
    public float HumeShield { get; set; } = 0f;
    public List<ItemType> Inventory { get; set; } = new();
    public Dictionary<ItemType, ushort> Ammo { get; set; } = new();
    public int SpawnChance { get; set; } = 100;
}
