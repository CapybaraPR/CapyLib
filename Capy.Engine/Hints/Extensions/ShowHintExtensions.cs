using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Models;
using Capy.Engine.Hints.Utilities;
using Exiled.API.Features;
using MEC;
using Hint = Capy.Engine.Hints.Models.Hint;

namespace Capy.Engine.Hints.Extensions;

/// <summary>
/// Набор удобных методов расширения для вывода подсказок и элементов интерфейса в различные зоны экрана.
/// Использует пиксельно-точную систему позиционирования AspectLib / SpecterLib.
/// </summary>
public static class ShowHintExtensions
{
    private static readonly ConcurrentDictionary<int, Dictionary<string, Hint>> TaggedHints = new();
    private static readonly ConcurrentDictionary<int, Dictionary<string, CoroutineHandle>> ExpiryHandles = new();

    /// <summary>
    /// Вывести подсказку в заданную зону экрана с точным расчетом экранных координат.
    /// </summary>
    public static void ShowZoneHint(this Player player, HintZone zone, string message, float duration = 3f, string tag = "default", int fontSize = 22)
    {
        if (player == null || !player.IsConnected) return;

        var display = PlayerDisplay.Get(player);
        KillExpiry(player.Id, tag);
        player.ClearCapyHint(tag);

        var (x, y, align, vAlign) = GetZoneCoordinates(zone);

        var hint = new Hint
        {
            Text = message,
            FontSize = fontSize,
            Tag = tag,
            XCoordinate = x,
            YCoordinate = y,
            Alignment = align,
            YCoordinateAlign = vAlign,
            Layer = HintLayer.Notification,
            Priority = 0,
            SyncSpeed = HintSyncSpeed.Fast
        };

        var dict = TaggedHints.GetOrAdd(player.Id, _ => new Dictionary<string, Hint>());
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
    /// Вывести стандартную подсказку по центру экрана (Notification).
    /// </summary>
    public static void ShowCapyHint(this Player player, string message, float duration = 3f, string tag = "default", int fontSize = 24)
    {
        player.ShowZoneHint(HintZone.Notification, message, duration, tag, fontSize);
    }

    /// <summary>
    /// Вывести широковещательное системное оповещение вверху экрана (TopCenter).
    /// </summary>
    public static void ShowAnnouncement(this Player player, string message, float duration = 5f, string title = "СИСТЕМНОЕ ОПОВЕЩЕНИЕ")
    {
        string formatted = $"<b><color=#ffa94e><size=24>[ {title} ]</size></color></b>\n<size=20><color=#ffffff>{message}</color></size>";
        player.ShowZoneHint(HintZone.TopCenter, formatted, duration, "system_announcement", 22);
    }

    /// <summary>
    /// Вывести широковещательное оповещение всем игрокам на сервере.
    /// </summary>
    public static void ShowAnnouncementToAll(string message, float duration = 5f, string title = "ОПОВЕЩЕНИЕ СЕРВЕРА")
    {
        foreach (var player in Player.List)
        {
            if (player != null && player.IsConnected)
                player.ShowAnnouncement(message, duration, title);
        }
    }

    /// <summary>
    /// Удалить подсказку по ее тегу.
    /// </summary>
    public static void ClearCapyHint(this Player player, string tag = "default")
    {
        if (player == null) return;

        KillExpiry(player.Id, tag);

        if (TaggedHints.TryGetValue(player.Id, out var dict))
        {
            if (dict.TryGetValue(tag, out var hint))
            {
                dict.Remove(tag);
                PlayerDisplay.Get(player).RemoveHint(hint);
            }
        }
    }

    /// <summary>
    /// Очистить все активные подсказки игрока.
    /// </summary>
    public static void CleanupPlayer(Player player)
    {
        if (player == null) return;

        if (ExpiryHandles.TryRemove(player.Id, out var handles))
        {
            foreach (var handle in handles.Values)
                Timing.KillCoroutines(handle);
            handles.Clear();
        }

        if (TaggedHints.TryRemove(player.Id, out var dict))
        {
            var display = PlayerDisplay.Get(player);
            foreach (var hint in dict.Values)
            {
                display.RemoveHint(hint);
            }
            dict.Clear();
        }
    }

    private static (float x, float y, HintAlignment align, HintVerticalAlign vAlign) GetZoneCoordinates(HintZone zone)
    {
        return zone switch
        {
            HintZone.TopCenter => (0f, 60f, HintAlignment.Center, HintVerticalAlign.Top),
            HintZone.UpperLeft => (-420f, 120f, HintAlignment.Left, HintVerticalAlign.Top),
            HintZone.UpperRight => (420f, 120f, HintAlignment.Right, HintVerticalAlign.Top),
            HintZone.LowerLeft => (-420f, 750f, HintAlignment.Left, HintVerticalAlign.Middle),
            HintZone.LowerCenter => (0f, 720f, HintAlignment.Center, HintVerticalAlign.Middle),
            HintZone.BottomCenter => (0f, 850f, HintAlignment.Center, HintVerticalAlign.Top),
            _ => (0f, 150f, HintAlignment.Center, HintVerticalAlign.Top)
        };
    }

    private static void StoreExpiry(int playerId, string tag, CoroutineHandle handle)
    {
        var dict = ExpiryHandles.GetOrAdd(playerId, _ => new Dictionary<string, CoroutineHandle>());
        dict[tag] = handle;
    }

    private static void KillExpiry(int playerId, string tag)
    {
        if (ExpiryHandles.TryGetValue(playerId, out var dict))
        {
            if (dict.TryGetValue(tag, out var handle))
            {
                Timing.KillCoroutines(handle);
                dict.Remove(tag);
            }
        }
    }

    private static void RemoveTaggedHint(int playerId, string tag, Hint hint)
    {
        if (TaggedHints.TryGetValue(playerId, out var dict))
        {
            if (dict.TryGetValue(tag, out var existing) && existing == hint)
            {
                dict.Remove(tag);
            }
        }
    }
}
