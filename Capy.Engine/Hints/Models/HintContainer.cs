using System;
using System.Collections.Generic;
using System.ComponentModel;
using Capy.Engine.Hints.Enum;

namespace Capy.Engine.Hints.Models;

public enum ContainerStackDirection
{
    Vertical,
    Horizontal
}

/// <summary>
/// Иерархический контейнер (Layout Container) для группировки дочерних элементов хинтов.
/// </summary>
public class HintContainer : AbstractHint
{
    private readonly List<AbstractHint> _children = new();
    private ContainerStackDirection _stackDirection = ContainerStackDirection.Vertical;
    private float _spacing = 4f;
    private HintAlignment _alignment = HintAlignment.Center;
    private HintVerticalAlign _yCoordinateAlign = HintVerticalAlign.Middle;
    private float _xCoordinate = 0f;
    private float _yCoordinate = 0f;

    public ContainerStackDirection StackDirection
    {
        get => _stackDirection;
        set
        {
            if (_stackDirection != value)
            {
                _stackDirection = value;
                RecalculateChildrenLayout();
                OnPropertyChanged(nameof(StackDirection));
            }
        }
    }

    public float Spacing
    {
        get => _spacing;
        set
        {
            if (Math.Abs(_spacing - value) > 0.01f)
            {
                _spacing = value;
                RecalculateChildrenLayout();
                OnPropertyChanged(nameof(Spacing));
            }
        }
    }

    public HintAlignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment != value)
            {
                _alignment = value;
                RecalculateChildrenLayout();
                OnPropertyChanged(nameof(Alignment));
            }
        }
    }

    public HintVerticalAlign YCoordinateAlign
    {
        get => _yCoordinateAlign;
        set
        {
            if (_yCoordinateAlign != value)
            {
                _yCoordinateAlign = value;
                RecalculateChildrenLayout();
                OnPropertyChanged(nameof(YCoordinateAlign));
            }
        }
    }

    public float XCoordinate
    {
        get => _xCoordinate;
        set
        {
            if (Math.Abs(_xCoordinate - value) > 0.01f)
            {
                _xCoordinate = value;
                RecalculateChildrenLayout();
                OnPropertyChanged(nameof(XCoordinate));
            }
        }
    }

    public float YCoordinate
    {
        get => _yCoordinate;
        set
        {
            if (Math.Abs(_yCoordinate - value) > 0.01f)
            {
                _yCoordinate = value;
                RecalculateChildrenLayout();
                OnPropertyChanged(nameof(YCoordinate));
            }
        }
    }

    public IReadOnlyList<AbstractHint> Children => _children.AsReadOnly();

    public void AddChild(AbstractHint child)
    {
        if (child == null || _children.Contains(child)) return;

        _children.Add(child);
        child.PropertyChanged += OnChildPropertyChanged;
        RecalculateChildrenLayout();
        OnPropertyChanged(nameof(Children));
    }

    public void RemoveChild(AbstractHint child)
    {
        if (child == null || !_children.Remove(child)) return;

        child.PropertyChanged -= OnChildPropertyChanged;
        RecalculateChildrenLayout();
        OnPropertyChanged(nameof(Children));
    }

    public void ClearChildren()
    {
        foreach (var child in _children)
        {
            child.PropertyChanged -= OnChildPropertyChanged;
        }
        _children.Clear();
        OnPropertyChanged(nameof(Children));
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RecalculateChildrenLayout();
        OnPropertyChanged(nameof(Children));
    }

    /// <summary>
    /// Пересчитать координаты дочерних элементов внутри контейнера.
    /// </summary>
    public void RecalculateChildrenLayout()
    {
        float currentY = YCoordinate;
        float currentX = XCoordinate;

        foreach (var child in _children)
        {
            if (child is Hint hintChild)
            {
                hintChild.Alignment = Alignment;
                hintChild.YCoordinateAlign = YCoordinateAlign;
                hintChild.XCoordinate = currentX;
                hintChild.YCoordinate = currentY;

                if (StackDirection == ContainerStackDirection.Vertical)
                {
                    float fontOffset = (child.FontSize * 1.2f) + Spacing;
                    currentY += fontOffset;
                }
                else
                {
                    currentX += Spacing;
                }
            }
            else if (child is HintContainer subContainer)
            {
                subContainer.XCoordinate = currentX;
                subContainer.YCoordinate = currentY;
                subContainer.RecalculateChildrenLayout();
            }
        }
    }
}
