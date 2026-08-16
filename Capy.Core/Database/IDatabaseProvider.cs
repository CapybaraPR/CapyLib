using Capy.Core.Database.Models;

namespace Capy.Core.Database;

/// <summary>
/// Унифицированный интерфейс провайдера базы данных.
/// </summary>
public interface IDatabaseProvider
{
    void Initialize();
    void Shutdown();

    PlayerDataModel? GetPlayer(string userId);
    void SavePlayer(PlayerDataModel player);
    IEnumerable<PlayerDataModel> GetAllPlayers();
    bool DeletePlayer(string userId);
}
