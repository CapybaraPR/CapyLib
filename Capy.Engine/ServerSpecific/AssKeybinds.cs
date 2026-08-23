using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace Capy.Engine.ServerSpecific;

public enum CustomKeybind
{
    Lmb,
    Rmb,
    E,
    F,
    T,
    B,
    G,
    Alt,
    R,
    UpArrow,
    DownArrow,
    LeftArrow,
    RightArrow,
    Backspace
}

/// <summary>
/// Система серверных биндов клавиш и кастомных настроек через ServerSpecificSettings.
/// </summary>
public static class AssKeybinds
{
    private static readonly Dictionary<CustomKeybind, (int Id, string Label, KeyCode DefaultKey)> KeyDefs = new()
    {
        { CustomKeybind.Lmb,        (1001, "ЛКМ",       KeyCode.Mouse0) },
        { CustomKeybind.Rmb,        (1002, "ПКМ",       KeyCode.Mouse1) },
        { CustomKeybind.E,          (1003, "E",         KeyCode.E) },
        { CustomKeybind.F,          (1004, "F",         KeyCode.F) },
        { CustomKeybind.T,          (1005, "T",         KeyCode.T) },
        { CustomKeybind.B,          (1006, "B",         KeyCode.B) },
        { CustomKeybind.G,          (1007, "G",         KeyCode.G) },
        { CustomKeybind.Alt,        (1008, "ALT",       KeyCode.LeftAlt) },
        { CustomKeybind.R,          (1009, "R",         KeyCode.R) },
        { CustomKeybind.UpArrow,    (1010, "⬆",         KeyCode.UpArrow) },
        { CustomKeybind.DownArrow,  (1011, "⬇",         KeyCode.DownArrow) },
        { CustomKeybind.LeftArrow,  (1012, "⬅",         KeyCode.LeftArrow) },
        { CustomKeybind.RightArrow, (1013, "➡",         KeyCode.RightArrow) },
        { CustomKeybind.Backspace,  (1014, "Backspace", KeyCode.Backspace) }
    };

    public static string GetLabel(CustomKeybind keybind)
    {
        if (KeyDefs.TryGetValue(keybind, out var def))
            return def.Label;
        return keybind.ToString();
    }

    public static Dictionary<CustomKeybind, SSKeybindSetting> RegisteredKeybinds { get; } = new();
    private static readonly Dictionary<int, CustomKeybind> IdToKeybindMap = new();

    public static event Action<Player, CustomKeybind>? OnKeybindPressed;
    public static event Action<Player, CustomKeybind>? OnKeybindReleased;

    public static void Initialize()
    {
        try
        {
            List<ServerSpecificSettingBase> existingSettings = ServerSpecificSettingsSync.DefinedSettings != null
                ? ServerSpecificSettingsSync.DefinedSettings.ToList()
                : new List<ServerSpecificSettingBase>();

            foreach (var kvp in KeyDefs)
            {
                CustomKeybind keybindEnum = kvp.Key;
                var (id, label, defaultKey) = kvp.Value;

                var setting = new SSKeybindSetting(
                    id,
                    label,
                    defaultKey,
                    true,
                    false,
                    $"Назначенная клавиша {label}"
                );

                RegisteredKeybinds[keybindEnum] = setting;
                IdToKeybindMap[id] = keybindEnum;

                if (!existingSettings.Any(s => s != null && s.SettingId == id))
                {
                    existingSettings.Add(setting);
                }
            }

            ServerSpecificSettingsSync.DefinedSettings = existingSettings.ToArray();
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnServerSettingValueReceived;

            Log.Info($"[AssKeybinds] Зарегистрировано {RegisteredKeybinds.Count} серверных клавиш в ServerSpecificSettings!");
        }
        catch (Exception ex)
        {
            Log.Error($"[AssKeybinds] Ошибка инициализации серверных клавиш: {ex}");
        }
    }

    private static void OnServerSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
    {
        if (hub == null || setting == null) return;

        if (IdToKeybindMap.TryGetValue(setting.SettingId, out CustomKeybind keybindEnum) && setting is SSKeybindSetting keybindSetting)
        {
            Player player = Player.Get(hub);
            if (player == null) return;

            if (keybindSetting.SyncIsPressed)
            {
                OnKeybindPressed?.Invoke(player, keybindEnum);
            }
            else
            {
                OnKeybindReleased?.Invoke(player, keybindEnum);
            }
        }
    }

    public static bool IsPressed(Player player, CustomKeybind keybind)
    {
        if (player == null || player.ReferenceHub == null) return false;
        if (!RegisteredKeybinds.TryGetValue(keybind, out var setting)) return false;

        if (ServerSpecificSettingsSync.TryGetSettingOfUser(player.ReferenceHub, setting.SettingId, out SSKeybindSetting userSetting))
        {
            return userSetting.SyncIsPressed;
        }

        return false;
    }
}
