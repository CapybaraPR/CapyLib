using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Hints;
using Exiled.API.Features;
using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Models;
using MEC;
using Mirror;
using UnityEngine;
using CustomHint = Capy.Engine.Hints.Models.Hint;

namespace Capy.Engine.Hints.Utilities;

public class PlayerDisplay
{
    private static readonly Dictionary<Player, PlayerDisplay> Displays = new();
    private static readonly object DisplaysLock = new();

    private readonly List<AbstractHint> _hints = new();
    private readonly object _hintsLock = new();
    private string _lastSentText = string.Empty;

    // Frame Batching & Rate Limiting (50ms ~ 20 FPS)
    private DateTime _lastFrameTime = DateTime.MinValue;
    private bool _isFramePending = false;
    private CoroutineHandle _batchCoroutine;
    private TimeSpan _minUpdateInterval = TimeSpan.FromMilliseconds(50);

    public Player Player { get; }

    private PlayerDisplay(Player player)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
    }

    public static PlayerDisplay Get(Player player)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));

        lock (DisplaysLock)
        {
            if (!Displays.TryGetValue(player, out var display))
            {
                display = new PlayerDisplay(player);
                Displays[player] = display;
            }
            return display;
        }
    }

    public static void RemovePlayer(Player player)
    {
        if (player == null) return;
        lock (DisplaysLock)
        {
            if (Displays.TryGetValue(player, out var display))
            {
                display.ClearHint();
                Displays.Remove(player);
            }
        }
    }

    public void AddHint(AbstractHint hint)
    {
        if (hint == null) return;

        lock (_hintsLock)
        {
            if (!_hints.Contains(hint))
            {
                _hints.Add(hint);
                hint.PropertyChanged += OnHintPropertyChanged;
            }
        }

        ScheduleUpdate();
    }

    public void RemoveHint(AbstractHint hint)
    {
        if (hint == null) return;

        lock (_hintsLock)
        {
            if (_hints.Remove(hint))
            {
                hint.PropertyChanged -= OnHintPropertyChanged;
            }
        }

        ScheduleUpdate();
    }

    public void ClearHint()
    {
        lock (_hintsLock)
        {
            foreach (var hint in _hints)
            {
                hint.PropertyChanged -= OnHintPropertyChanged;
            }
            _hints.Clear();
        }

        ScheduleUpdate();
    }

    public List<AbstractHint> GetHints()
    {
        lock (_hintsLock)
        {
            return new List<AbstractHint>(_hints);
        }
    }

    public void SetMinUpdateInterval(TimeSpan interval)
    {
        if (interval.TotalMilliseconds >= 0)
        {
            _minUpdateInterval = interval;
        }
    }

    public void ForceUpdate(bool force = true)
    {
        UpdateDisplayInternal();
    }

    private void OnHintPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ScheduleUpdate();
    }

    /// <summary>
    /// Пакетное обновление кадра с ограничениями частоты кадров (Frame Batching & Rate Limiting).
    /// </summary>
    private void ScheduleUpdate()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastFrameTime;

        if (elapsed >= _minUpdateInterval)
        {
            _lastFrameTime = now;
            UpdateDisplayInternal();
        }
        else if (!_isFramePending)
        {
            _isFramePending = true;
            float delay = (float)(_minUpdateInterval - elapsed).TotalSeconds;
            if (delay < 0.01f) delay = 0.01f;

            _batchCoroutine = Timing.CallDelayed(delay, () =>
            {
                _isFramePending = false;
                _lastFrameTime = DateTime.UtcNow;
                UpdateDisplayInternal();
            });
        }
    }

    /// <summary>
    /// Конвертировать Y-координату с учетом типа привязки (Top, Middle, Bottom).
    /// </summary>
    private float GetYCoordinateAsTop(CustomHint hint, string text)
    {
        float y = hint.YCoordinate;
        int lineCount = text.Split('\n').Length;
        float textHeight = hint.FontSize * 1.2f * Math.Max(1, lineCount);

        switch (hint.YCoordinateAlign)
        {
            case HintVerticalAlign.Top:
                return y;
            case HintVerticalAlign.Middle:
                return y - textHeight / 2f;
            case HintVerticalAlign.Bottom:
                return y - textHeight;
            default:
                return y;
        }
    }

    private void UpdateDisplayInternal()
    {
        if (Player == null || Player.ReferenceHub == null)
            return;

        if (Player.Role == null || Player.Role.Type == PlayerRoles.RoleTypeId.None)
            return;

        var conn = Player.ReferenceHub.connectionToClient;
        if (conn == null || !conn.isReady)
            return;

        List<CustomHint> hintsToRender = new List<CustomHint>();

        lock (_hintsLock)
        {
            foreach (var abstractHint in _hints)
            {
                if (abstractHint == null || abstractHint.Hide) continue;

                if (abstractHint is CustomHint singleHint)
                {
                    hintsToRender.Add(singleHint);
                }
                else if (abstractHint is HintContainer container)
                {
                    container.RecalculateChildrenLayout();
                    foreach (var child in container.Children)
                    {
                        if (child is CustomHint childHint && !childHint.Hide)
                        {
                            hintsToRender.Add(childHint);
                        }
                    }
                }
            }
        }

        if (hintsToRender.Count == 0)
        {
            if (_lastSentText != string.Empty)
            {
                _lastSentText = string.Empty;
                try
                {
                    conn.Send(new HintMessage(new TextHint(string.Empty, new HintParameter[0], new HintEffect[0], 0.1f)));
                }
                catch { }
            }
            return;
        }

        // Сортировка хинтов по слою (Layer) и приоритету (Priority)
        hintsToRender = hintsToRender
            .OrderBy(h => (int)h.Layer)
            .ThenByDescending(h => h.Priority)
            .ToList();

        var sb = new StringBuilder(1024);

        // Старт с верхнего якоря (Top Anchor)
        sb.AppendLine("<line-height=0><voffset=9999>P</voffset>");

        foreach (var hint in hintsToRender)
        {
            if (string.IsNullOrEmpty(hint.Text))
                continue;

            // Применяем авто-перенос строк при включенном AutoWrap
            string formattedText = hint.AutoWrap
                ? TextMeasurement.WrapText(hint.Text, hint.FontSize, hint.MaxPixelWidth)
                : hint.Text;

            float yTop = GetYCoordinateAsTop(hint, formattedText);
            float voffset = 700f - yTop;

            sb.AppendFormat("<size={0}>", hint.FontSize);

            if (hint.Alignment != HintAlignment.Center)
            {
                switch (hint.Alignment)
                {
                    case HintAlignment.Left:
                        sb.Append("<align=left>");
                        break;
                    case HintAlignment.Right:
                        sb.Append("<align=right>");
                        break;
                }
            }

            string[] lines = formattedText.Split('\n');
            foreach (var line in lines)
            {
                float lineHeight = hint.FontSize * 1.2f;
                voffset -= lineHeight;

                if (hint.XCoordinate != 0f)
                {
                    sb.AppendFormat("<pos={0:0.#}>", hint.XCoordinate);
                }
                sb.Append("<line-height=0>");
                if (voffset != 0f)
                {
                    sb.AppendFormat("<voffset={0:0.#}>", voffset);
                }
                sb.Append(line);
                if (voffset != 0f)
                {
                    sb.Append("</voffset>");
                }
                sb.AppendLine();
            }

            if (hint.Alignment != HintAlignment.Center)
            {
                sb.Append("</align>");
            }
            sb.Append("</size>");
        }

        // Нижний якорь (Bottom Anchor)
        sb.AppendLine("<line-height=0><voffset=-9999>P</voffset>");

        // Автоматическое восстановление и закрытие сломанных тегов RichText
        string rawText = sb.ToString();
        string fullText = HintStringBuilder.RepairUnclosedTags(rawText);

        if (_lastSentText == fullText)
            return;

        _lastSentText = fullText;

        try
        {
            var message = new HintMessage(
                new TextHint(
                    fullText,
                    new HintParameter[] { new StringHintParameter(string.Empty) },
                    new HintEffect[] { new AlphaEffect(1f, 0f, 1f) },
                    99999f
                )
            );
            conn.Send(message);
        }
        catch { }
    }
}
