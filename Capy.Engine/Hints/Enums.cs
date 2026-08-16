namespace Capy.Engine.Hints;

public enum HintAlignment
{
    Left,
    Center,
    Right
}

public enum HintVerticalAlign
{
    Top,
    Middle,
    Bottom
}

public enum HintLayer
{
    Background = 0,
    Gameplay = 10,
    HUD = 20,
    Notification = 30,
    Overlay = 40,
    Critical = 50
}

public enum HintSyncSpeed
{
    Slow = 250,
    Normal = 100,
    Fast = 50
}
