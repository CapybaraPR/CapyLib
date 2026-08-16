using Capy.Core.Database.Models;
using LiteDB;

namespace Capy.Core.Database;

/// <summary>
/// Локальный провайдер базы данных на базе LiteDB.
/// </summary>
public class LiteDbProvider : IDatabaseProvider
{
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

    public void Initialize()
    {
        try
        {
            _db = new LiteDatabase(_dbPath);
            _players = _db.GetCollection<PlayerDataModel>("players");
            _players.EnsureIndex(x => x.Id, true);
            Log.Info($"[LiteDbProvider] База данных LiteDB успешно инициализирована: {_dbPath}");
        }
        catch (Exception ex)
        {
            Log.Error($"[LiteDbProvider] Ошибка инициализации LiteDB: {ex.Message}");
        }
    }

    public void Shutdown()
    {
        try
        {
            _db?.Dispose();
            _db = null;
        }
        catch { }
    }

    public PlayerDataModel? GetPlayer(string userId)
    {
        if (string.IsNullOrEmpty(userId) || _players == null) return null;
        return _players.FindById(userId);
    }

    public void SavePlayer(PlayerDataModel player)
    {
        if (player == null || string.IsNullOrEmpty(player.Id) || _players == null) return;
        _players.Upsert(player);
    }

    public IEnumerable<PlayerDataModel> GetAllPlayers()
    {
        if (_players == null) return Enumerable.Empty<PlayerDataModel>();
        return _players.FindAll();
    }

    public bool DeletePlayer(string userId)
    {
        if (string.IsNullOrEmpty(userId) || _players == null) return false;
        return _players.Delete(userId);
    }
}
