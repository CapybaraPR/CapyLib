using Capy.Core.Database.Models;

namespace Capy.Core.Database;

public interface IDatabaseProvider
{
    bool IsInitialized { get; }

    void Initialize();
    void Shutdown();

    PlayerDataModel? GetPlayer(string userId);
    void SavePlayer(PlayerDataModel player);
    IEnumerable<PlayerDataModel> GetAllPlayers();
    IReadOnlyList<PlayerDataModel> GetTopPlayers(string fieldName, int limit);
    bool DeletePlayer(string userId);
}
