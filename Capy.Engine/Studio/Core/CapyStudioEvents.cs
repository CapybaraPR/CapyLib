using System;
using Exiled.API.Features;
using UnityEngine;

namespace Capy.Engine.Studio.Core;

/// <summary>
/// Система событий и хуков CapyStudio для взаимодействия других плагинов со схематиками и картами.
/// </summary>
public static class CapyStudioEvents
{
    public static event Action<string, Vector3>? SchematicSpawning;
    public static event Action<SchematicObject>? SchematicSpawned;
    public static event Action<SchematicObject>? SchematicDestroyed;
    public static event Action<Player, string>? ButtonInteracted;

    internal static void InvokeSchematicSpawning(string name, Vector3 pos)
    {
        try { SchematicSpawning?.Invoke(name, pos); } catch (Exception ex) { Log.Error($"[CapyStudio Events] {ex}"); }
    }

    internal static void InvokeSchematicSpawned(SchematicObject schem)
    {
        try { SchematicSpawned?.Invoke(schem); } catch (Exception ex) { Log.Error($"[CapyStudio Events] {ex}"); }
    }

    internal static void InvokeSchematicDestroyed(SchematicObject schem)
    {
        try { SchematicDestroyed?.Invoke(schem); } catch (Exception ex) { Log.Error($"[CapyStudio Events] {ex}"); }
    }

    internal static void InvokeButtonInteracted(Player player, string tag)
    {
        try { ButtonInteracted?.Invoke(player, tag); } catch (Exception ex) { Log.Error($"[CapyStudio Events] {ex}"); }
    }
}
