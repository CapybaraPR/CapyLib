using System;
using System.Collections.Generic;

namespace Capy.API.DiscordBridge;

/// <summary>
/// Снимок XP/уровня игрока для внешних потребителей (Discord-бот).
/// </summary>
public sealed class XpSnapshot
{
    public float Xp { get; set; }
    public string LevelText { get; set; } = string.Empty;
    public string LevelColor { get; set; } = "#ffffff";
}

/// <summary>
/// Запись лидерборда опыта.
/// </summary>
public sealed class XpLeaderboardEntry
{
    public string UserId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public float Xp { get; set; }
    public string LevelText { get; set; } = string.Empty;
    public string LevelColor { get; set; } = "#ffffff";
}

/// <summary>
/// Хук для серверных плагинов (например, CapyLib.NoRules): регистрирует поставщиков данных XP,
/// которые доступны Bridge API (`GET v1/xp`, `GET v1/xp/leaderboard`) для Discord-бота.
/// </summary>
public static class BridgeXpRegistry
{
    /// <summary>Возвращает снимок XP игрока по UserId (steam id или discord-linked id). null — нет данных.</summary>
    public static Func<string, XpSnapshot?>? GetXp { get; set; }

    /// <summary>Возвращает топ игроков по опыту. null — система выключена.</summary>
    public static Func<int, List<XpLeaderboardEntry>?>? GetLeaderboard { get; set; }

    /// <summary>
    /// Снимает всех поставщиков (вызывается при выгрузке плагина-провайдера).
    /// </summary>
    public static void Clear()
    {
        GetXp = null;
        GetLeaderboard = null;
    }
}
