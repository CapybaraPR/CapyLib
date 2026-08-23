using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using AdminToys;
using UnityEngine;

namespace Capy.Engine.Studio.Models;

/// <summary>
/// Сериализуемый вектор координат (X, Y, Z).
/// </summary>
public sealed class SerializableVector3
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }

    public SerializableVector3() { }

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public SerializableVector3(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3 ToUnityVector3() => new(x, y, z);
    public Quaternion ToUnityQuaternion() => Quaternion.Euler(x, y, z);
}

public enum BlockType
{
    Empty = 0,
    Primitive = 1,
    LightSource = 2,
    Door = 3,
    Workstation = 4,
    ItemSpawnPoint = 5,
    RagdollSpawnPoint = 6,
    PlayerSpawnPoint = 7,
    ShootingTarget = 8,
    Teleport = 9,
    Capybara = 10,
    Text = 11,
    Locker = 12,
    Camera079 = 13,
    Waypoint = 14,
    Interactable = 15
}

/// <summary>
/// Описание отдельного блока внутри схематики.
/// </summary>
public sealed class BlockData
{
    public string Name { get; set; } = "Block";
    public int ObjectId { get; set; }
    public int ParentId { get; set; }
    public SerializableVector3 Position { get; set; } = new();
    public SerializableVector3 Rotation { get; set; } = new();
    public SerializableVector3 Scale { get; set; } = new(1f, 1f, 1f);
    public int BlockType { get; set; } = 1;
    public Dictionary<string, object> Properties { get; set; } = new();

    public PrimitiveType GetPrimitiveType()
    {
        if (Properties.TryGetValue("PrimitiveType", out var val) && val != null)
        {
            if (val is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Number)
                {
                    if (je.TryGetInt32(out int num))
                        return (PrimitiveType)num;
                    if (je.TryGetSingle(out float frac))
                        return (PrimitiveType)(int)frac;
                }
                if (je.ValueKind == JsonValueKind.String && int.TryParse(je.GetString(), out int parsedStr))
                    return (PrimitiveType)parsedStr;
            }
            if (val is long l) return (PrimitiveType)(int)l;
            if (val is int i) return (PrimitiveType)i;
            if (int.TryParse(val.ToString(), out int parsed)) return (PrimitiveType)parsed;
        }
        return PrimitiveType.Cube;
    }

    public Color GetColor()
    {
        if (Properties.TryGetValue("Color", out var val) && val != null)
        {
            if (val is JsonElement je && je.ValueKind != JsonValueKind.String)
                return Color.white;

            string hex = val.ToString()?.Trim() ?? string.Empty;
            if (!hex.StartsWith("#") && (hex.Length == 6 || hex.Length == 8))
                hex = "#" + hex;

            if (ColorUtility.TryParseHtmlString(hex, out var color))
                return color;
        }
        return Color.white;
    }

    public PrimitiveFlags GetPrimitiveFlags()
    {
        if (Properties.TryGetValue("PrimitiveFlags", out var val) && val != null)
        {
            if (val is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Number)
                {
                    if (je.TryGetInt32(out int num))
                        return (PrimitiveFlags)(byte)num;
                    if (je.TryGetSingle(out float frac))
                        return (PrimitiveFlags)(byte)frac;
                }
                if (je.ValueKind == JsonValueKind.String && byte.TryParse(je.GetString(), out byte parsedStr))
                    return (PrimitiveFlags)parsedStr;
            }
            if (val is long l) return (PrimitiveFlags)(byte)l;
            if (val is int i) return (PrimitiveFlags)(byte)i;
            if (byte.TryParse(val.ToString(), out byte parsed)) return (PrimitiveFlags)parsed;
        }
        return PrimitiveFlags.Visible | PrimitiveFlags.Collidable;
    }

    public float GetLightIntensity()
    {
        if (Properties.TryGetValue("Intensity", out var val) && val != null)
        {
            if (val is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetSingle(out float num))
                return num;
            if (float.TryParse(val.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                return parsed;
        }
        return 1.0f;
    }

    public float GetLightRange()
    {
        if (Properties.TryGetValue("Range", out var val) && val != null)
        {
            if (val is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetSingle(out float num))
                return num;
            if (float.TryParse(val.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                return parsed;
        }
        return 5.0f;
    }

    public bool GetLightShadows()
    {
        if (Properties.TryGetValue("Shadows", out var val) && bool.TryParse(val?.ToString(), out bool parsed))
            return parsed;
        return true;
    }

    public string GetTextContent()
    {
        if (Properties.TryGetValue("Text", out var val) || Properties.TryGetValue("Content", out val))
            return val?.ToString() ?? "Text";
        return "CapyStudio Text";
    }

    public float GetTextSize()
    {
        if (Properties.TryGetValue("TextSize", out var val) && float.TryParse(val?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            return parsed;
        return 1.0f;
    }

    public float GetTeleportCooldown()
    {
        if (Properties.TryGetValue("Cooldown", out var val) && float.TryParse(val?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            return parsed;
        return 2.0f;
    }

    public string GetTeleportId()
    {
        if (Properties.TryGetValue("TeleportId", out var val) || Properties.TryGetValue("Id", out val))
        {
            string? s = val switch
            {
                JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
                _ => val?.ToString()
            };
            if (!string.IsNullOrWhiteSpace(s))
                return s!;
        }
        return Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    public List<string> GetTeleportTargets()
    {
        var result = new List<string>();

        if (Properties.TryGetValue("Targets", out var val) && val != null)
        {
            IEnumerable<string?> items;

            if (val is JsonElement je && je.ValueKind == JsonValueKind.Array)
            {
                // System.Text.Json кладёт в Dictionary<string, object> именно JsonElement —
                // проверка "is IEnumerable<object>" на нём не срабатывает.
                items = je.EnumerateArray()
                    .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() : e.GetRawText());
            }
            else if (val is IEnumerable<object> list)
            {
                items = list.Select(o => o?.ToString());
            }
            else
            {
                items = new[] { val.ToString() };
            }

            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                    result.Add(item!);
            }
        }

        return result;
    }

    public string GetDoorType()
    {
        if (Properties.TryGetValue("DoorType", out var val))
            return val?.ToString() ?? "LCZ";
        return "LCZ";
    }

    public string GetLockerType()
    {
        if (Properties.TryGetValue("LockerType", out var val))
            return val?.ToString() ?? "PedestalScp500";
        return "PedestalScp500";
    }

    public string GetShootingTargetType()
    {
        if (Properties.TryGetValue("TargetType", out var val))
            return val?.ToString() ?? "Sport";
        return "Sport";
    }
}

/// <summary>
/// Полная модель схематики (100% совместимость с MapEditorReborn и Studio .json форматом).
/// </summary>
public sealed class SchematicData
{
    public int RootObjectId { get; set; } = 10000;
    public List<BlockData> Blocks { get; set; } = new();
}
