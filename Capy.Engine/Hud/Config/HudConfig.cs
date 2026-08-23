using System.ComponentModel;

namespace Capy.Engine.Hud.Config;

/// <summary>
/// Настройки экранного HUD и информационных панелей игроков.
/// </summary>
public sealed class HudConfig
{
    [Description("Включен ли экранный HUD.")]
    public bool Enabled { get; set; } = true;

    [Description("Отображать время текущего раунда.")]
    public bool ShowRoundTime { get; set; } = true;

    [Description("Отображать список зрителей (наблюдателей) за живым игроком.")]
    public bool ShowSpectatorList { get; set; } = true;

    [Description("Отображать подробную нижнюю панель для наблюдателей (за кем смотрит, HP, предметы).")]
    public bool ShowSpectatorBottomPanel { get; set; } = true;

    [Description("Отображать таймер детонации Альфа-боеголовки при её запуске.")]
    public bool ShowWarheadStatus { get; set; } = true;

    [Description("Отображать статус активных генераторов (0/3).")]
    public bool ShowGeneratorStatus { get; set; } = true;

    [Description("Отображать таймеры подкрепления МОГ / Хаос для наблюдателей.")]
    public bool ShowRespawnTimers { get; set; } = true;

    [Description("Частота обновления панелей HUD в секундах.")]
    public float UpdateIntervalSeconds { get; set; } = 0.5f;
}
