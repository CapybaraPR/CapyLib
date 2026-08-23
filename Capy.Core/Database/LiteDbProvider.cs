using Capy.Core.Database.Models;
using LiteDB;

namespace Capy.Core.Database;

public class LiteDbProvider : IDatabaseProvider
{
    private const int MaxTopLimit = 100;

    private LiteDatabase? _db;
    private ILiteCollection<PlayerDataModel>? _players;
    private readonly string _dbPath;

    public LiteDbProvider()
    {
        var dir = Path.Combine(Paths.Plugins, "CapyLib", "Database");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        _dbPath = Path.Combine(dir, "CapyData.db");
    }

    public bool IsInitialized => _players != null;

    public void Initialize()
    {
        try
        {
            _db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
            _players = _db.GetCollection<PlayerDataModel>("players");
            _players.EnsureIndex(x => x.Id, true);
            _players.EnsureIndex(x => x.Kills);
            _players.EnsureIndex(x => x.TotalPlaytimeSeconds);
            _players.EnsureIndex(x => x.RoundsPlayed);
            Log.Info($"[LiteDbProvider] База данных LiteDB успешно инициализирована (Shared mode): {_dbPath}");
        }
        catch (Exception ex)
        {
            _db = null;
            _players = null;
            Log.Error($"[LiteDbProvider] Ошибка инициализации LiteDB: {ex}");
        }
    }

    public void Shutdown()
    {
        try
        {
            _players = null;
            _db?.Dispose();
            _db = null;
        }
        catch { }
    }

    public PlayerDataModel? GetPlayer(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return null;

        if (_players == null)
        {
            Log.Warn("[LiteDbProvider] Чтение отклонено: база не инициализирована.");
            return null;
        }

        return _players.FindById(userId);
    }

    public void SavePlayer(PlayerDataModel player)
    {
        if (player == null || string.IsNullOrEmpty(player.Id)) return;

        if (_players == null)
        {
            Log.Warn($"[LiteDbProvider] ЗАПИСЬ ПОТЕРЯНА (база не инициализирована): {player.Id}");
            return;
        }

        _players.Upsert(player);
    }

    public IEnumerable<PlayerDataModel> GetAllPlayers()
    {
        return _players?.FindAll() ?? Enumerable.Empty<PlayerDataModel>();
    }

    public IReadOnlyList<PlayerDataModel> GetTopPlayers(string fieldName, int limit)
    {
        if (limit <= 0 || string.IsNullOrWhiteSpace(fieldName))
            return Array.Empty<PlayerDataModel>();

        if (_players == null)
        {
            Log.Warn("[LiteDbProvider] Выборка топа отклонена: база не инициализирована.");
            return Array.Empty<PlayerDataModel>();
        }

        try
        {
            return _players
                .Find(global::LiteDB.Query.All(fieldName, global::LiteDB.Query.Descending), limit: Math.Min(limit, MaxTopLimit))
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error($"[LiteDbProvider] Ошибка выборки топа по '{fieldName}': {ex.Message}");
            return Array.Empty<PlayerDataModel>();
        }
    }

    public bool DeletePlayer(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        if (_players == null)
        {
            Log.Warn("[LiteDbProvider] Удаление отклонено: база не инициализирована.");
            return false;
        }

        return _players.Delete(userId);
    }

    public ILiteCollection<T>? GetCollection<T>(string name)
    {
        return _db?.GetCollection<T>(name);
    }
}
