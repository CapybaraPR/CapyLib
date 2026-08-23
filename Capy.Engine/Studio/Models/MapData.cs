using System;
using System.Collections.Generic;
using Exiled.API.Enums;

namespace Capy.Engine.Studio.Models;

/// <summary>
/// Описание привязки схематики или объекта к конкретной комнате карты.
/// </summary>
public sealed class MapEntryData
{
    public string SchematicName { get; set; } = string.Empty;
    public RoomType TargetRoom { get; set; } = RoomType.Unknown;
    public SerializableVector3 LocalPosition { get; set; } = new();
    public SerializableVector3 LocalRotation { get; set; } = new();
    public SerializableVector3 Scale { get; set; } = new(1f, 1f, 1f);
}

/// <summary>
/// Модель карты, состоящей из нескольких схематик и декораций.
/// </summary>
public sealed class MapData
{
    public string MapName { get; set; } = "CustomMap";
    public string Author { get; set; } = "CapyStudio";
    public string Description { get; set; } = string.Empty;
    public List<MapEntryData> Entries { get; set; } = new();
    public List<BlockData> GlobalBlocks { get; set; } = new();
}
