namespace Capy.Engine.Hints.Service;

/// <summary>
/// Базовый класс элемента интерфейса подсказки на экране игрока.
/// </summary>
public class AbstractHint
{
    public string Text { get; set; } = string.Empty;
    public int FontSize { get; set; } = 24;
    public float XCoordinate { get; set; } = 0f;
    public float YCoordinate { get; set; } = 540f;
    public HintVerticalAlign YCoordinateAlign { get; set; } = HintVerticalAlign.Middle;
    public HintAlignment Alignment { get; set; } = HintAlignment.Center;
    public string Tag { get; set; } = "default";
    public HintZone Zone { get; set; } = HintZone.Notification;
    public HintLayer Layer { get; set; } = HintLayer.Notification;
    public int Priority { get; set; } = 0;
    public HintSyncSpeed SyncSpeed { get; set; } = HintSyncSpeed.Fast;
}
