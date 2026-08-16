using UnityEngine;

namespace Capy.Engine.Effects;

public class GlowConfig
{
    public Func<ushort, bool> ItemCheck { get; }
    public Color Color { get; }
    public float Intensity { get; }
    public float Radius { get; }
    public bool GlowInHand { get; }
    public bool GlowOnFloor { get; }

    public GlowConfig(Func<ushort, bool> itemCheck, Color color, float intensity = 0.7f, float radius = 0.5f, bool glowInHand = true, bool glowOnFloor = true)
    {
        ItemCheck = itemCheck;
        Color = color;
        Intensity = intensity;
        Radius = radius;
        GlowInHand = glowInHand;
        GlowOnFloor = glowOnFloor;
    }
}

/// <summary>
/// Система свечения и визуальных эффектов для кастомных предметов.
/// </summary>
public static class ItemGlowSystem
{
    private static readonly List<GlowConfig> Configs = new();

    public static void AddGlow(Func<ushort, bool> itemCheck, Color color, float intensity = 0.7f, float radius = 0.5f, bool glowInHand = true, bool glowOnFloor = true)
    {
        if (itemCheck == null) return;
        Configs.Add(new GlowConfig(itemCheck, color, intensity, radius, glowInHand, glowOnFloor));
    }

    public static void AddGlowHex(Func<ushort, bool> itemCheck, string colorHex, float intensity = 0.7f, float radius = 0.5f, bool glowInHand = true, bool glowOnFloor = true)
    {
        if (!colorHex.StartsWith("#")) colorHex = "#" + colorHex;
        if (!ColorUtility.TryParseHtmlString(colorHex, out Color color)) color = Color.white;

        AddGlow(itemCheck, color, intensity, radius, glowInHand, glowOnFloor);
    }

    public static void Clear()
    {
        Configs.Clear();
    }
}
