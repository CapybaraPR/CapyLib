using System;
using System.ComponentModel;
using Capy.Engine.Hints.Enum;

namespace Capy.Engine.Hints.Models;

public abstract class AbstractHint : INotifyPropertyChanged
{
    private string _text = string.Empty;
    private int _fontSize = 20;
    private bool _hide;
    private HintSyncSpeed _syncSpeed = HintSyncSpeed.Fast;
    private HintLayer _layer = HintLayer.Notification;
    private int _priority = 0;
    private bool _autoWrap = true;
    private float _maxPixelWidth = 1000f;

    public Guid Id { get; } = Guid.NewGuid();
    public string Tag { get; set; } = "default";

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? string.Empty;
                OnPropertyChanged(nameof(Text));
            }
        }
    }

    public int FontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize != value)
            {
                _fontSize = value;
                OnPropertyChanged(nameof(FontSize));
            }
        }
    }

    public bool Hide
    {
        get => _hide;
        set
        {
            if (_hide != value)
            {
                _hide = value;
                OnPropertyChanged(nameof(Hide));
            }
        }
    }

    public HintSyncSpeed SyncSpeed
    {
        get => _syncSpeed;
        set
        {
            if (_syncSpeed != value)
            {
                _syncSpeed = value;
                OnPropertyChanged(nameof(SyncSpeed));
            }
        }
    }

    /// <summary>
    /// Слои приоритета отображения подсказки (HUD, Notification, Alert, Critical).
    /// </summary>
    public HintLayer Layer
    {
        get => _layer;
        set
        {
            if (_layer != value)
            {
                _layer = value;
                OnPropertyChanged(nameof(Layer));
            }
        }
    }

    /// <summary>
    /// Вес приоритета внутри одного слоя (чем выше число, тем выше приоритет).
    /// </summary>
    public int Priority
    {
        get => _priority;
        set
        {
            if (_priority != value)
            {
                _priority = value;
                OnPropertyChanged(nameof(Priority));
            }
        }
    }

    /// <summary>
    /// Автоматически расставлять переносы строк при превышении MaxPixelWidth.
    /// </summary>
    public bool AutoWrap
    {
        get => _autoWrap;
        set
        {
            if (_autoWrap != value)
            {
                _autoWrap = value;
                OnPropertyChanged(nameof(AutoWrap));
            }
        }
    }

    /// <summary>
    /// Максимальная ширина подсказки в пикселях для переноса строк (по умолчанию 1000px).
    /// </summary>
    public float MaxPixelWidth
    {
        get => _maxPixelWidth;
        set
        {
            if (Math.Abs(_maxPixelWidth - value) > 0.01f)
            {
                _maxPixelWidth = value;
                OnPropertyChanged(nameof(MaxPixelWidth));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
