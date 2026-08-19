using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Capy.Core.Database.Models;
using Exiled.API.Features;
using LiteDB;

namespace Capy.API.DiscordBridge;

/// <summary>
/// Сервис персистентного реестра персонала (Staff Registry) и аналитики активности.
/// </summary>
public sealed class StaffService : IDisposable
{
    private static readonly Regex SteamIdRegex = new(@"^(?:7656\d{13})(?:@steam)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _sessionStartTimes = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _onlineNicknames = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();
    private readonly string _dbPath;
    private LiteDatabase? _db;
    private ILiteCollection<StaffMemberModel>? _collection;

    public StaffService()
    {
        string dir = Path.Combine(Paths.Plugins, "CapyLib", "Database");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _dbPath = Path.Combine(dir, "CapyData.db");
        Initialize();
    }

    public void Initialize()
    {
        lock (_sync)
        {
            try
            {
                if (CapyPlugin.Instance?.Database is LiteDbProvider provider)
                {
                    _collection = provider.GetCollection<StaffMemberModel>("staff");
                }

                if (_collection == null)
                {
                    _db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
                    _collection = _db.GetCollection<StaffMemberModel>("staff");
                }

                _collection.EnsureIndex(x => x.Id, true);
                _collection.EnsureIndex(x => x.DiscordUserId, false);
                _collection.EnsureIndex(x => x.IsActive, false);
                Log.Info($"[StaffService] База данных персонала успешно инициализирована (CapyData.db): {_dbPath}");
            }
            catch (Exception ex)
            {
                Log.Error($"[StaffService] Ошибка инициализации LiteDB: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            try
            {
                _db?.Dispose();
                _db = null;
                _collection = null;
            }
            catch { }
        }
    }

    public static string NormalizeUserId(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string trimmed = input.Trim();
        if (Regex.IsMatch(trimmed, @"^\d{17}$"))
            return $"{trimmed}@steam";

        return trimmed.ToLowerInvariant();
    }

    public bool IsValidUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;
        string normalized = NormalizeUserId(userId);
        return SteamIdRegex.IsMatch(normalized) || normalized.EndsWith("@discord") || normalized.EndsWith("@northwood");
    }

    public StaffMemberModel? GetStaff(string userId)
    {
        lock (_sync)
        {
            try
            {
                if (_collection == null) return null;
                string id = NormalizeUserId(userId);
                return _collection.FindById(id);
            }
            catch (Exception ex)
            {
                Log.Error($"[StaffService] Ошибка GetStaff({userId}): {ex.Message}");
                return null;
            }
        }
    }

    public StaffMemberModel? GetStaffByDiscordId(ulong discordId)
    {
        if (discordId == 0) return null;
        lock (_sync)
        {
            try
            {
                if (_collection == null) return null;
                return _collection.FindOne(x => x.DiscordUserId == discordId && x.IsActive);
            }
            catch (Exception ex)
            {
                Log.Error($"[StaffService] Ошибка GetStaffByDiscordId({discordId}): {ex.Message}");
                return null;
            }
        }
    }

    public List<StaffMemberModel> GetAllStaff(bool activeOnly = true)
    {
        lock (_sync)
        {
            List<StaffMemberModel> list;
            try
            {
                if (_collection == null)
                {
                    Initialize();
                    if (_collection == null) return new List<StaffMemberModel>();
                }

                list = activeOnly
                    ? _collection.Find(x => x.IsActive).ToList()
                    : _collection.FindAll().ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"[StaffService] Ошибка GetAllStaff: {ex.Message}");
                return new List<StaffMemberModel>();
            }

            // Real-time enrich online staff from memory cache (0ms latency, thread-safe)
            try
            {
                DateTime now = DateTime.UtcNow;
                foreach (var st in list)
                {
                    if (_sessionStartTimes.TryGetValue(st.Id, out DateTime startTime))
                    {
                        st.LastSeenUtc = now;
                        if (_onlineNicknames.TryGetValue(st.Id, out string? nick) && !string.IsNullOrWhiteSpace(nick))
                            st.Nickname = nick;

                        long ongoingSeconds = (long)(now - startTime).TotalSeconds;
                        if (ongoingSeconds > 0)
                        {
                            st.WeeklyPlaytimeSeconds += ongoingSeconds;
                            st.TotalPlaytimeSeconds += ongoingSeconds;
                        }
                    }
                }
            }
            catch { }

            return list;
        }
    }

    public StaffMemberModel AddOrUpdateStaff(
        string rawUserId,
        ulong discordUserId,
        string discordUserName,
        string group,
        string scope,
        ulong actorDiscordId,
        string actorDiscordName,
        string reason)
    {
        string userId = NormalizeUserId(rawUserId);
        if (!IsValidUserId(userId))
            throw new ArgumentException($"Некорректный UserId: {rawUserId}. Требуется SteamID64 (например, 76561198...).");

        if (string.IsNullOrWhiteSpace(group))
            throw new ArgumentException("Группа не может быть пустой.");

        string normalizedScope = string.IsNullOrWhiteSpace(scope) ? "all" : scope.Trim().ToLowerInvariant();

        lock (_sync)
        {
            if (_collection == null)
                throw new InvalidOperationException("База данных персонала не инициализирована.");

            DateTime now = DateTime.UtcNow;
            StaffMemberModel? existing = _collection.FindById(userId);

            if (existing == null)
            {
                existing = new StaffMemberModel
                {
                    Id = userId,
                    DiscordUserId = discordUserId,
                    DiscordUserName = discordUserName ?? string.Empty,
                    Group = group,
                    ServerScope = normalizedScope,
                    IsActive = true,
                    AssignedByDiscordId = actorDiscordId,
                    AssignedByDiscordName = actorDiscordName ?? string.Empty,
                    AssignedAtUtc = now,
                    FirstSeenUtc = now,
                    LastSeenUtc = now,
                    LastWeekResetUtc = now,
                    History = new List<StaffHistoryEntry>
                    {
                        new StaffHistoryEntry
                        {
                            TimestampUtc = now,
                            Action = "appointed",
                            OldGroup = string.Empty,
                            NewGroup = group,
                            ActorDiscordId = actorDiscordId,
                            ActorDiscordName = actorDiscordName ?? string.Empty,
                            Reason = reason ?? "Назначение в состав"
                        }
                    }
                };
                _collection.Insert(existing);
                Log.Info($"[StaffService] Назначен новый администратор: {userId} ({group}, scope: {normalizedScope}) актором {actorDiscordName} ({actorDiscordId})");
            }
            else
            {
                string oldGroup = existing.Group;
                bool wasActive = existing.IsActive;

                existing.Group = group;
                existing.ServerScope = normalizedScope;
                existing.IsActive = true;
                if (discordUserId != 0) existing.DiscordUserId = discordUserId;
                if (!string.IsNullOrWhiteSpace(discordUserName)) existing.DiscordUserName = discordUserName;

                existing.History.Add(new StaffHistoryEntry
                {
                    TimestampUtc = now,
                    Action = !wasActive ? "restored" : (oldGroup != group ? "rank_updated" : "scope_updated"),
                    OldGroup = oldGroup,
                    NewGroup = group,
                    ActorDiscordId = actorDiscordId,
                    ActorDiscordName = actorDiscordName ?? string.Empty,
                    Reason = reason ?? "Обновление прав"
                });

                _collection.Update(existing);
                Log.Info($"[StaffService] Обновлён администратор: {userId} (было: {oldGroup} -> стало: {group}, scope: {normalizedScope})");
            }

            // Immediately apply to online player if present
            ApplyToPlayerIfOnline(userId, group, normalizedScope);
            return existing;
        }
    }

    public bool RemoveStaff(
        string rawUserId,
        ulong actorDiscordId,
        string actorDiscordName,
        string reason)
    {
        string userId = NormalizeUserId(rawUserId);
        lock (_sync)
        {
            if (_collection == null) return false;
            StaffMemberModel? existing = _collection.FindById(userId);
            if (existing == null || !existing.IsActive) return false;

            DateTime now = DateTime.UtcNow;
            string oldGroup = existing.Group;
            existing.IsActive = false;
            existing.History.Add(new StaffHistoryEntry
            {
                TimestampUtc = now,
                Action = "removed",
                OldGroup = oldGroup,
                NewGroup = "none",
                ActorDiscordId = actorDiscordId,
                ActorDiscordName = actorDiscordName ?? string.Empty,
                Reason = reason ?? "Снятие с должности"
            });

            _collection.Update(existing);
            Log.Info($"[StaffService] Снят администратор: {userId} (был {oldGroup}) актором {actorDiscordName} ({actorDiscordId})");

            // Strip in-game group if player online
            StripPlayerIfOnline(userId);
            return true;
        }
    }

    public void OnPlayerVerified(Player player)
    {
        if (player == null || !player.IsConnected || player.IsHost || player.IsNPC ||
            !player.IsVerified || string.IsNullOrWhiteSpace(player.UserId))
            return;

        string id = NormalizeUserId(player.UserId);
        StaffMemberModel? staff = GetStaff(id);
        if (staff == null || !staff.IsActive)
            return;

        // Check scope
        if (!IsScopeApplicable(staff.ServerScope))
            return;

        lock (_sync)
        {
            DateTime now = DateTime.UtcNow;
            CheckWeeklyReset(staff, now);
            staff.LastSeenUtc = now;
            if (!string.IsNullOrWhiteSpace(player.Nickname))
            {
                staff.Nickname = player.Nickname;
                _onlineNicknames[id] = player.Nickname;
            }

            _collection?.Update(staff);
            _sessionStartTimes[id] = now;
        }

        UserGroup? targetGroup = ServerStatic.PermissionsHandler.GetGroup(staff.Group.Trim());
        if (targetGroup != null)
        {
            player.Group = targetGroup;
            Log.Info($"[StaffService] Администратору {player.Nickname} ({id}) автоматически выдана группа '{staff.Group}' из реестра персонала.");
        }
        else
        {
            Log.Warn($"[StaffService] Группа '{staff.Group}' для администратора {player.Nickname} ({id}) не найдена в ServerStatic.PermissionsHandler!");
        }
    }

    public void OnPlayerLeft(string rawUserId, long sessionSeconds, bool wasDuty)
    {
        string id = NormalizeUserId(rawUserId);
        _sessionStartTimes.TryRemove(id, out _);
        _onlineNicknames.TryRemove(id, out _);
        lock (_sync)
        {
            if (_collection == null) return;
            StaffMemberModel? staff = _collection.FindById(id);
            if (staff == null) return;

            DateTime now = DateTime.UtcNow;
            CheckWeeklyReset(staff, now);
            staff.TotalPlaytimeSeconds += sessionSeconds;
            staff.WeeklyPlaytimeSeconds += sessionSeconds;
            if (wasDuty)
                staff.DutyPlaytimeSeconds += sessionSeconds;
            staff.LastSeenUtc = now;

            _collection.Update(staff);
        }
    }

    public void RecordAction(string rawUserId, string actionType)
    {
        string id = NormalizeUserId(rawUserId);
        lock (_sync)
        {
            if (_collection == null) return;
            StaffMemberModel? staff = _collection.FindById(id);
            if (staff == null) return;

            switch (actionType.ToLowerInvariant())
            {
                case "ban":
                    staff.BansCount++;
                    break;
                case "mute":
                    staff.MutesCount++;
                    break;
                case "kick":
                    staff.KicksCount++;
                    break;
            }

            _collection.Update(staff);
        }
    }

    private void CheckWeeklyReset(StaffMemberModel staff, DateTime now)
    {
        // Reset weekly playtime if a new Monday / 7 days passed
        if ((now - staff.LastWeekResetUtc).TotalDays >= 7)
        {
            staff.WeeklyPlaytimeSeconds = 0;
            staff.LastWeekResetUtc = now;
        }
    }

    private static bool IsScopeApplicable(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope) || scope.Equals("all", StringComparison.OrdinalIgnoreCase))
            return true;

        int port = Server.Port;
        if (port == 7777 && (scope.Equals("nr", StringComparison.OrdinalIgnoreCase) || scope.Equals("norules", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (port == 7778 && (scope.Equals("mrp", StringComparison.OrdinalIgnoreCase) || scope.Equals("mediumrp", StringComparison.OrdinalIgnoreCase)))
            return true;

        return false;
    }

    private void ApplyToPlayerIfOnline(string userId, string group, string scope)
    {
        if (!IsScopeApplicable(scope)) return;
        Player? player = Player.List.FirstOrDefault(p => NormalizeUserId(p.UserId) == userId);
        if (player != null && player.IsConnected && !player.IsHost)
        {
            UserGroup? targetGroup = ServerStatic.PermissionsHandler.GetGroup(group.Trim());
            if (targetGroup != null)
            {
                player.Group = targetGroup;
                Log.Info($"[StaffService] Онлайн-игроку {player.Nickname} ({userId}) мгновенно назначена группа '{group}'.");
            }

            try
            {
                StaffMemberModel? staff = _collection?.FindById(userId);
                if (staff != null)
                {
                    if (!string.IsNullOrWhiteSpace(player.Nickname))
                        staff.Nickname = player.Nickname;
                    staff.LastSeenUtc = DateTime.UtcNow;
                    _collection?.Update(staff);
                }
                _sessionStartTimes.TryAdd(userId, DateTime.UtcNow);
                if (!string.IsNullOrWhiteSpace(player.Nickname))
                    _onlineNicknames[userId] = player.Nickname;
            }
            catch { }
        }
    }

    private void StripPlayerIfOnline(string userId)
    {
        Player? player = Player.List.FirstOrDefault(p => NormalizeUserId(p.UserId) == userId);
        if (player != null && player.IsConnected && !player.IsHost)
        {
            player.Group = null!;
            player.RankName = null;
            player.RankColor = null;
            Log.Info($"[StaffService] Онлайн-игрок {player.Nickname} ({userId}) мгновенно лишён группы.");
        }
    }
}