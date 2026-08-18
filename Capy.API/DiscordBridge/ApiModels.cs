using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Capy.API.DiscordBridge;

public sealed class DiscordRoleMapping
{
    public string Group { get; set; } = string.Empty;
    public int Priority { get; set; } = 100;
    public string Operator { get; set; } = "or";
    public List<ulong> DiscordRoleIds { get; set; } = new();
}

public sealed class DiscordLinkRecord
{
    public string GameUserId { get; set; } = string.Empty;
    public ulong DiscordUserId { get; set; }
    public string DiscordUserName { get; set; } = string.Empty;
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastVerifiedAtUtc { get; set; } = DateTime.UtcNow;
    public List<ulong> DiscordRoleIds { get; set; } = new();
    public string? ManagedExiledGroup { get; set; }
}

public sealed class PendingDiscordLink
{
    public string Code { get; set; } = string.Empty;
    public ulong DiscordUserId { get; set; }
    public string DiscordUserName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class LinkCodeRequest
{
    [JsonPropertyName("discord_user_id")]
    public ulong DiscordUserId { get; set; }

    [JsonPropertyName("discord_user_name")]
    public string DiscordUserName { get; set; } = string.Empty;
}

public sealed class LinkCodeResponse
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("expires_in_seconds")]
    public int ExpiresInSeconds { get; set; }

    [JsonPropertyName("expires_at")]
    public string ExpiresAt { get; set; } = string.Empty;

    [JsonPropertyName("instruction")]
    public string Instruction { get; set; } = string.Empty;
}

public sealed class LinkConfirmRequest
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("game_user_id")]
    public string GameUserId { get; set; } = string.Empty;
}

public sealed class LinkedAccountItem
{
    [JsonPropertyName("game_user_id")]
    public string GameUserId { get; set; } = string.Empty;

    [JsonPropertyName("discord_user_id")]
    public ulong DiscordUserId { get; set; }

    [JsonPropertyName("discord_user_name")]
    public string DiscordUserName { get; set; } = string.Empty;

    [JsonPropertyName("linked_at")]
    public string LinkedAt { get; set; } = string.Empty;

    [JsonPropertyName("roles")]
    public List<ulong> Roles { get; set; } = new();

    [JsonPropertyName("current_group")]
    public string? CurrentGroup { get; set; }
}

public sealed class LinkedAccountsResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("accounts")]
    public List<LinkedAccountItem> Accounts { get; set; } = new();
}

public sealed class LinkRoleSyncRequest
{
    [JsonPropertyName("discord_user_id")]
    public ulong DiscordUserId { get; set; }

    [JsonPropertyName("roles")]
    public List<ulong> Roles { get; set; } = new();
}

public sealed class LinkRoleSyncResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("game_user_id")]
    public string? GameUserId { get; set; }

    [JsonPropertyName("applied_group")]
    public string? AppliedGroup { get; set; }
}

public sealed class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";

    [JsonPropertyName("plugin_name")]
    public string PluginName { get; set; } = "CapyLib";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.1.0";

    [JsonPropertyName("uptime_seconds")]
    public double UptimeSeconds { get; set; }

    [JsonPropertyName("game_thread_responsive")]
    public bool GameThreadResponsive { get; set; }

    [JsonPropertyName("auth_mode")]
    public string AuthMode { get; set; } = "ssh_signature";
}

public sealed class ServerStatus
{
    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("public_address")]
    public string PublicAddress { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public ushort Port { get; set; }

    [JsonPropertyName("players_count")]
    public int PlayersCount { get; set; }

    [JsonPropertyName("max_players")]
    public int MaxPlayers { get; set; }

    [JsonPropertyName("is_round_running")]
    public bool IsRoundRunning { get; set; }

    [JsonPropertyName("is_round_started")]
    public bool IsRoundStarted { get; set; }

    [JsonPropertyName("is_round_ended")]
    public bool IsRoundEnded { get; set; }

    [JsonPropertyName("is_waiting_for_players")]
    public bool IsWaitingForPlayers { get; set; }

    [JsonPropertyName("is_warhead_detonated")]
    public bool IsWarheadDetonated { get; set; }

    [JsonPropertyName("is_warhead_in_progress")]
    public bool IsWarheadInProgress { get; set; }

    [JsonPropertyName("is_friendly_fire_enabled")]
    public bool IsFriendlyFireEnabled { get; set; }

    [JsonPropertyName("round_duration_seconds")]
    public int RoundDurationSeconds { get; set; }

    [JsonPropertyName("tps")]
    public double Tps { get; set; }
}

public sealed class StatusResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("online")]
    public int Online { get; set; }

    [JsonPropertyName("maximum")]
    public int Maximum { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("round_state")]
    public string RoundState { get; set; } = "lobby";

    [JsonPropertyName("round_time")]
    public string RoundTime { get; set; } = "00:00";

    [JsonPropertyName("tps")]
    public double Tps { get; set; } = 60.0;

    [JsonPropertyName("utc")]
    public string Utc { get; set; } = string.Empty;

    [JsonPropertyName("server")]
    public ServerStatus Server { get; set; } = new();

    [JsonPropertyName("scps_alive")]
    public int ScpsAlive { get; set; }

    [JsonPropertyName("humans_alive")]
    public int HumansAlive { get; set; }

    [JsonPropertyName("spectators_count")]
    public int SpectatorsCount { get; set; }
}

public sealed class PlayerItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nickname")]
    public string Nickname { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("team")]
    public string Team { get; set; } = string.Empty;

    [JsonPropertyName("is_alive")]
    public bool IsAlive { get; set; }

    [JsonPropertyName("is_cuffed")]
    public bool IsCuffed { get; set; }

    [JsonPropertyName("is_godmode")]
    public bool IsGodmode { get; set; }

    [JsonPropertyName("group")]
    public string? Group { get; set; }

    [JsonPropertyName("rank")]
    public string? Rank { get; set; }

    [JsonPropertyName("rank_color")]
    public string? RankColor { get; set; }

    [JsonPropertyName("custom_info")]
    public string? CustomInfo { get; set; }

    [JsonPropertyName("ping")]
    public int Ping { get; set; }
}

public sealed class PlayersResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("online")]
    public int Online { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("maximum")]
    public int Maximum { get; set; }

    [JsonPropertyName("max_players")]
    public int MaxPlayers { get; set; }

    [JsonPropertyName("players")]
    public List<PlayerItem> Players { get; set; } = new();
}

public sealed class GroupItem
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("badge_text")]
    public string BadgeText { get; set; } = string.Empty;

    [JsonPropertyName("badge_color")]
    public string BadgeColor { get; set; } = string.Empty;

    [JsonPropertyName("permissions_bitmask")]
    public ulong PermissionsBitmask { get; set; }

    [JsonPropertyName("cover")]
    public bool Cover { get; set; }

    [JsonPropertyName("hidden")]
    public bool Hidden { get; set; }
}

public sealed class GroupsResponse
{
    [JsonPropertyName("groups")]
    public List<GroupItem> Groups { get; set; } = new();
}

public sealed class CommandRequest
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("access")]
    public string Access { get; set; } = "ra";

    [JsonPropertyName("actor_name")]
    public string ActorName { get; set; } = string.Empty;

    [JsonPropertyName("actor_id")]
    public ulong ActorId { get; set; }
}

public sealed class CommandResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("execution_time_ms")]
    public double ExecutionTimeMs { get; set; }
}

public enum BridgeLogCategory
{
    Punishments,
    Rounds,
    Server,
    Commands,
    Reports
}

public sealed class BridgeLogField
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("inline")]
    public bool Inline { get; set; } = true;
}

public sealed class BridgeLogEvent
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public string Level { get; set; } = "info";

    [JsonPropertyName("fields")]
    public List<BridgeLogField> Fields { get; set; } = new();
}

public sealed class LogEventBatchResponse
{
    [JsonPropertyName("events")]
    public List<BridgeLogEvent> Events { get; set; } = new();

    [JsonPropertyName("latest_id")]
    public long LatestId { get; set; }

    [JsonPropertyName("oldest_id")]
    public long OldestId { get; set; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
}

public sealed class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

public sealed class StaffAddRequest
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("discord_user_id")]
    public ulong DiscordUserId { get; set; }

    [JsonPropertyName("discord_user_name")]
    public string DiscordUserName { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("server_scope")]
    public string ServerScope { get; set; } = "all";

    [JsonPropertyName("actor_discord_id")]
    public ulong ActorDiscordId { get; set; }

    [JsonPropertyName("actor_discord_name")]
    public string ActorDiscordName { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public sealed class StaffRemoveRequest
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("actor_discord_id")]
    public ulong ActorDiscordId { get; set; }

    [JsonPropertyName("actor_discord_name")]
    public string ActorDiscordName { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public sealed class StaffListResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("staff")]
    public List<Capy.Core.Database.Models.StaffMemberModel> Staff { get; set; } = new();
}

public sealed class StaffMemberResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("member")]
    public Capy.Core.Database.Models.StaffMemberModel? Member { get; set; }
}

