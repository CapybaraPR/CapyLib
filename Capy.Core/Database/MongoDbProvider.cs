using Capy.Core.Database.Models;
using MongoDB.Driver;

namespace Capy.Core.Database;

/// <summary>
/// Провайдер базы данных на базе MongoDB для распределённых серверов.
/// </summary>
public class MongoDbProvider : IDatabaseProvider
{
    private readonly string _connectionString;
    private readonly string _databaseName;
    private IMongoDatabase? _database;
    private IMongoCollection<PlayerDataModel>? _players;

    public MongoDbProvider(string connectionString, string databaseName)
    {
        _connectionString = connectionString;
        _databaseName = databaseName;
    }

    public void Initialize()
    {
        try
        {
            var client = new MongoClient(_connectionString);
            _database = client.GetDatabase(_databaseName);
            _players = _database.GetCollection<PlayerDataModel>("players");
            Log.Info($"[MongoDbProvider] Подключение к MongoDB '{_databaseName}' успешно установлено.");
        }
        catch (Exception ex)
        {
            Log.Error($"[MongoDbProvider] Ошибка подключения к MongoDB: {ex.Message}");
        }
    }

    public void Shutdown()
    {
        _database = null;
        _players = null;
    }

    public PlayerDataModel? GetPlayer(string userId)
    {
        if (string.IsNullOrEmpty(userId) || _players == null) return null;
        try
        {
            return _players.Find(p => p.Id == userId).FirstOrDefault();
        }
        catch (Exception ex)
        {
            Log.Error($"[MongoDbProvider] Ошибка получения игрока {userId}: {ex.Message}");
            return null;
        }
    }

    public void SavePlayer(PlayerDataModel player)
    {
        if (player == null || string.IsNullOrEmpty(player.Id) || _players == null) return;
        try
        {
            _players.ReplaceOne(p => p.Id == player.Id, player, new ReplaceOptions { IsUpsert = true });
        }
        catch (Exception ex)
        {
            Log.Error($"[MongoDbProvider] Ошибка сохранения игрока {player.Id}: {ex.Message}");
        }
    }

    public IEnumerable<PlayerDataModel> GetAllPlayers()
    {
        if (_players == null) return Enumerable.Empty<PlayerDataModel>();
        try
        {
            return _players.Find(_ => true).ToList();
        }
        catch (Exception ex)
        {
            Log.Error($"[MongoDbProvider] Ошибка получения всех игроков: {ex.Message}");
            return Enumerable.Empty<PlayerDataModel>();
        }
    }

    public bool DeletePlayer(string userId)
    {
        if (string.IsNullOrEmpty(userId) || _players == null) return false;
        try
        {
            var result = _players.DeleteOne(p => p.Id == userId);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            Log.Error($"[MongoDbProvider] Ошибка удаления игрока {userId}: {ex.Message}");
            return false;
        }
    }
}
