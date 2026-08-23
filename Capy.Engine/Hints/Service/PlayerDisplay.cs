using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Exiled.API.Features;

namespace Capy.Engine.Hints.Service;

/// <summary>
/// Компоновщик и рендерер экранного интерфейса HUD и подсказок для конкретного игрока.
/// </summary>
public class PlayerDisplay
{
    private static readonly ConcurrentDictionary<int, PlayerDisplay> Displays = new();

    public Player Player { get; }
    private readonly List<AbstractHint> _hints = new();
    private readonly object _lock = new();

    public PlayerDisplay(Player player)
    {
        Player = player;
    }

    public static PlayerDisplay Get(Player player)
    {
        return Displays.GetOrAdd(player.Id, _ => new PlayerDisplay(player));
    }

    public static void Remove(Player player)
    {
        Remove(player.Id);
    }

    public static void Remove(int playerId)
    {
        if (Displays.TryRemove(playerId, out var display))
            display.ClearHint();
    }

    public void AddHint(AbstractHint hint)
    {
        lock (_lock)
        {
            _hints.Add(hint);
            Render();
        }
    }

    public void RemoveHint(AbstractHint hint)
    {
        lock (_lock)
        {
            if (_hints.Remove(hint))
                Render();
        }
    }

    public void ClearHint()
    {
        lock (_lock)
        {
            _hints.Clear();
            if (Player != null && Player.IsConnected)
                Player.ShowHint(string.Empty, 0.1f);
        }
    }

    public void Render()
    {
        if (Player == null || !Player.IsConnected) return;

        var sb = new StringBuilder();
        List<AbstractHint> activeHints;

        lock (_lock)
        {
            activeHints = _hints.OrderBy(h => (int)h.Layer).ThenBy(h => h.Priority).ToList();
        }

        if (activeHints.Count == 0)
        {
            Player.ShowHint(string.Empty, 0.1f);
            return;
        }

        // Группировка подсказок по зонам экрана
        var upperLeft = activeHints.Where(h => h.Zone == HintZone.UpperLeft).ToList();
        var upperRight = activeHints.Where(h => h.Zone == HintZone.UpperRight).ToList();
        var notifications = activeHints.Where(h => h.Zone == HintZone.Notification).ToList();
        var lowerCenter = activeHints.Where(h => h.Zone == HintZone.LowerCenter).ToList();
        var lowerLeft = activeHints.Where(h => h.Zone == HintZone.LowerLeft).ToList();
        var bottomCenter = activeHints.Where(h => h.Zone == HintZone.BottomCenter).ToList();

        // 1. Верхние зоны (Чат / Отряд)
        if (upperLeft.Count > 0 || upperRight.Count > 0)
        {
            foreach (var hint in upperLeft)
                sb.AppendLine($"<align=left><pos=2%><size={hint.FontSize}>{hint.Text}</size></pos></align>");

            foreach (var hint in upperRight)
                sb.AppendLine($"<align=right><pos=98%><size={hint.FontSize}>{hint.Text}</size></pos></align>");
        }

        // 2. Центральные уведомления / Бродкасты
        if (notifications.Count > 0)
        {
            foreach (var hint in notifications)
                sb.AppendLine($"<align=center><size={hint.FontSize}>{hint.Text}</size></align>");
        }

        // 3. Карточка предмета в руках (LowerCenter)
        if (lowerCenter.Count > 0)
        {
            foreach (var hint in lowerCenter)
                sb.AppendLine($"<align=center><size={hint.FontSize}>{hint.Text}</size></align>");
        }

        // 4. Кулдауны способностей (LowerLeft)
        if (lowerLeft.Count > 0)
        {
            foreach (var hint in lowerLeft)
                sb.AppendLine($"<align=left><pos=2%><size={hint.FontSize}>{hint.Text}</size></pos></align>");
        }

        // 5. Подбор / выброс предметов (BottomCenter)
        if (bottomCenter.Count > 0)
        {
            foreach (var hint in bottomCenter)
                sb.AppendLine($"<align=center><size={hint.FontSize}>{hint.Text}</size></align>");
        }

        string finalContent = sb.ToString().TrimEnd();
        if (!string.IsNullOrEmpty(finalContent))
        {
            Player.ShowHint(finalContent, 2.5f);
        }
        else
        {
            Player.ShowHint(string.Empty, 0.1f);
        }
    }
}
