using Capy.Engine.Hints.Enum;

namespace Capy.Engine.Hints.Models;

public class Hint : AbstractHint
{
    private HintAlignment _alignment = HintAlignment.Center;
    private HintVerticalAlign _yCoordinateAlign = HintVerticalAlign.Middle;
    private float _xCoordinate = 0f;
    private float _yCoordinate = 0f;

    public HintAlignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment != value)
            {
                _alignment = value;
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
                OnPropertyChanged(nameof(YCoordinateAlign));
            }
        }
    }

    public float XCoordinate
    {
        get => _xCoordinate;
        set
        {
            if (_xCoordinate != value)
            {
                _xCoordinate = value;
                OnPropertyChanged(nameof(XCoordinate));
            }
        }
    }

    public float YCoordinate
    {
        get => _yCoordinate;
        set
        {
            if (_yCoordinate != value)
            {
                _yCoordinate = value;
                OnPropertyChanged(nameof(YCoordinate));
            }
        }
    }
}
