using System;
using System.Collections.Generic;

namespace Capy.Core.Database.Models;

/// <summary>
/// Запись об изменении ранга или статуса в истории администратора.
/// </summary>
public class StaffHistoryEntry
{
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = string.Empty;
    public string OldGroup { get; set; } = string.Empty;
    public string NewGroup { get; set; } = string.Empty;
    public ulong ActorDiscordId { get; set; }
    public string ActorDiscordName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Персистентная модель администратора/модератора в базе данных.
/// </summary>
public class StaffMemberModel
{
    public string Id { get; set; } = string.Empty; // Game UserId (e.g. 76561198...@steam)
    public string Nickname { get; set; } = string.Empty;
    public ulong DiscordUserId { get; set; }
    public string DiscordUserName { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;
    public string ServerScope { get; set; } = "all"; // "all", "nr", "mrp"
    public bool IsActive { get; set; } = true;

    public ulong AssignedByDiscordId { get; set; }
    public string AssignedByDiscordName { get; set; } = string.Empty;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public long TotalPlaytimeSeconds { get; set; }
    public long WeeklyPlaytimeSeconds { get; set; }
    public long DutyPlaytimeSeconds { get; set; }
    public DateTime LastWeekResetUtc { get; set; } = DateTime.UtcNow;

    public int BansCount { get; set; }
    public int MutesCount { get; set; }
    public int KicksCount { get; set; }

    public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;

    public List<StaffHistoryEntry> History { get; set; } = new();
}