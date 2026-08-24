using Capy.Core.Database.Models;
using MongoDB.Driver;

namespace Capy.Core.Database;

public class MongoDbProvider : IDatabaseProvider
{
    private const int MaxTopLimit = 100;

    private readonly string _connectionString;
    private readonly string _databaseName;
    private MongoClient? _client;
    private IMongoDatabase? _database;
    private IMongoCollection<PlayerDataModel>? _players;

    public MongoDbProvider(string connectionString, string databaseName)
    {
        _connectionString = connectionString;
        _databaseName = databaseName;
    }

    public bool IsInitialized => _players != null;

    public void Initialize()
    {
        try
        {
            _client = new MongoClient(_connectionString);
            _database = _client.GetDatabase(_databaseName);
            _players = _database.GetCollection<PlayerDataModel>("players");
            _players.Indexes.CreateOne(new CreateIndexModel<PlayerDataModel>(Builders<PlayerDataModel>.IndexKeys.Descending(x => x.Xp)));
            Log.Info($"[MongoDbProvider] Коллекция '{_databaseName}.players' инициализирована (подключение устанавливается лениво драйвером).");
        }
        catch (Exception ex)
        {
            Log.Error($"[MongoDbProvider] Ошибка подключения к MongoDB: {ex.Message}");
        }
    }

    public void Shutdown()
    {
        _players = null;
        _database = null;

        try
        {
            (_client?.Cluster as IDisposable)?.Dispose();
        }
        catch { }

        _client = null;
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

    public IReadOnlyList<PlayerDataModel> GetTopPlayers(string fieldName, int limit)
    {
        if (limit <= 0 || string.IsNullOrWhiteSpace(fieldName) || _players == null)
            return Array.Empty<PlayerDataModel>();

        try
        {
            var sort = new SortDefinitionBuilder<PlayerDataModel>().Descending(fieldName);
            return _players
                .Find(Builders<PlayerDataModel>.Filter.Empty)
                .Sort(sort)
                .Limit(Math.Min(limit, MaxTopLimit))
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error($"[MongoDbProvider] Ошибка выборки топа по '{fieldName}': {ex.Message}");
            return Array.Empty<PlayerDataModel>();
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
