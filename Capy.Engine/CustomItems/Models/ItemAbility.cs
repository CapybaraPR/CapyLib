namespace Capy.Engine.CustomItems.Models;

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic,
    Admin
}

public class ItemAbility
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Keybind { get; set; } = "F";
    public float Cooldown { get; set; } = 5f;
}
