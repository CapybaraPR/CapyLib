using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Capy.Engine.Hints.Service;
using Exiled.API.Features;
using MEC;

namespace Capy.Engine.Hints.Extensions;

/// <summary>
/// Набор удобных методов расширения для вывода подсказок и элементов интерфейса в различные зоны экрана.
/// </summary>
public static class ShowHintExtensions
{
    private static readonly ConcurrentDictionary<int, Dictionary<string, AbstractHint>> TaggedHints = new();
    private static readonly ConcurrentDictionary<int, Dictionary<string, CoroutineHandle>> ExpiryHandles = new();

    /// <summary>
    /// Вывести подсказку в заданную зону экрана (UpperLeft, UpperRight, LowerLeft, LowerCenter, BottomCenter, Notification).
    /// </summary>
    public static void ShowZoneHint(this Player player, HintZone zone, string message, float duration = 3f, string tag = "default", int fontSize = 22)
    {
        if (player == null || !player.IsConnected) return;

        var display = PlayerDisplay.Get(player);
        KillExpiry(player.Id, tag);
        player.ClearCapyHint(tag);

        var hint = new AbstractHint
        {
            Text = message,
            FontSize = fontSize,
            Tag = tag,
            Zone = zone,
            Layer = HintLayer.Notification,
            Priority = 0
        };

        var dict = TaggedHints.GetOrAdd(player.Id, _ => new Dictionary<string, AbstractHint>());
        dict[tag] = hint;

        display.AddHint(hint);

        if (duration > 0f && duration < 3600f)
        {
            CoroutineHandle handle = Timing.CallDelayed(duration, () =>
            {
                RemoveTaggedHint(player.Id, tag, hint);
                display.RemoveHint(hint);
            });

            StoreExpiry(player.Id, tag, handle);
        }
    }

    /// <summary>
    /// Вывести стандартную подсказку (Notification / Center).
    /// </summary>
    public static void ShowCapyHint(this Player player, string message, float duration = 3f, string tag = "default", int fontSize = 24)
    {
        player.ShowZoneHint(HintZone.Notification, message, duration, tag, fontSize);
    }

    /// <summary>
    /// Карточка предмета в руках (LowerCenter): название, описание и привязки клавиш.
    /// </summary>
    public static void ShowItemInfo(this Player player, string title, string description, string keybinds = "", float duration = 3.5f)
    {
        string text = $"<color=#ffa94e><b>{title}</b></color>";
        if (!string.IsNullOrEmpty(description))
            text += $"\n<color=#c2c2c2><size=18>{description}</size></color>";
        if (!string.IsNullOrEmpty(keybinds))
            text += $"\n<color=#ffd285><size=16>{keybinds}</size></color>";

        player.ShowZoneHint(HintZone.LowerCenter, text, duration, "item_info", 20);
    }

    /// <summary>
    /// Отображение кулдауна способности или предмета (LowerLeft).
    /// </summary>
    public static void ShowAbilityCooldown(this Player player, string abilityName, float remainingSeconds, float totalSeconds)
    {
        int progressPercent = totalSeconds > 0 ? (int)((1f - (remainingSeconds / totalSeconds)) * 100f) : 100;
        string bar = GetProgressBar(progressPercent);
        string text = $"<color=#ffd285><b>{abilityName}</b></color> <color=#ffa94e>{remainingSeconds:F1}с</color>\n{bar}";

        player.ShowZoneHint(HintZone.LowerLeft, text, 1.2f, $"cd_{abilityName}", 18);
    }

    /// <summary>
    /// Уведомление о подборе / выбросе предмета (BottomCenter).
    /// </summary>
    public static void ShowItemPickup(this Player player, string itemName, bool isPickup = true, float duration = 2.0f)
    {
        string icon = isPickup ? "➕ <color=#a3e635>Получено:</color>" : "➖ <color=#f87171>Выброшено:</color>";
        string text = $"{icon} <color=#ffffff><b>{itemName}</b></color>";

        player.ShowZoneHint(HintZone.BottomCenter, text, duration, "pickup_toast", 19);
    }

    /// <summary>
    /// Отображение хитмаркера и урона при стрельбе (BottomCenter).
    /// </summary>
    public static void ShowHitmarker(this Player player, float damage, bool isKill = false)
    {
        string text = isKill
            ? "<color=#ff0000><b>💀 УБИТ!</b></color>"
            : $"<color=#ff4444><b>-{(int)damage} HP</b></color>";

        player.ShowZoneHint(HintZone.BottomCenter, text, 1.0f, "hitmarker", 20);
    }

    /// <summary>
    /// Вывод важного серверного оповещения через HUD (полная замена Map.Broadcast / Player.Broadcast).
    /// </summary>
    public static void ShowAnnouncement(this Player player, string message, float duration = 5.0f, string tag = "server_announcement")
    {
        player.ShowZoneHint(HintZone.Notification, message, duration, tag, 24);
    }

    /// <summary>
    /// Отправить объявление всем игрокам сервера через HUD.
    /// </summary>
    public static void ShowAnnouncementToAll(string message, float duration = 5.0f, string tag = "server_announcement")
    {
        foreach (var player in Player.List)
        {
            if (player != null && player.IsConnected)
                player.ShowAnnouncement(message, duration, tag);
        }
    }

    public static void ClearCapyHint(this Player player, string tag)
    {
        if (player == null || string.IsNullOrEmpty(tag)) return;

        if (TaggedHints.TryGetValue(player.Id, out var dict))
        {
            if (dict.TryGetValue(tag, out var hint) && PlayerDisplay.Get(player) is { } display)
                display.RemoveHint(hint);

            dict.Remove(tag);
        }

        KillExpiry(player.Id, tag);
    }

    public static void CleanupPlayer(int playerId)
    {
        TaggedHints.TryRemove(playerId, out _);
        if (ExpiryHandles.TryRemove(playerId, out var dict))
        {
            foreach (var handle in dict.Values)
                Timing.KillCoroutines(handle);
        }
    }

    private static void RemoveTaggedHint(int playerId, string tag, AbstractHint hint)
    {
        if (TaggedHints.TryGetValue(playerId, out var dict))
        {
            if (dict.TryGetValue(tag, out var existing) && ReferenceEquals(existing, hint))
                dict.Remove(tag);
        }
    }

    private static void StoreExpiry(int playerId, string tag, CoroutineHandle handle)
    {
        var dict = ExpiryHandles.GetOrAdd(playerId, _ => new Dictionary<string, CoroutineHandle>());
        dict[tag] = handle;
    }

    private static void KillExpiry(int playerId, string tag)
    {
        if (ExpiryHandles.TryGetValue(playerId, out var dict) && dict.TryGetValue(tag, out var handle))
        {
            Timing.KillCoroutines(handle);
            dict.Remove(tag);
        }
    }

    private static string GetProgressBar(int percent, int totalSegments = 10)
    {
        percent = Math.Max(0, Math.Min(100, percent));
        int filled = (percent * totalSegments) / 100;
        int empty = totalSegments - filled;

        return $"<color=#a3e635>{new string('■', filled)}</color><color=#555555>{new string('■', empty)}</color>";
    }
}
