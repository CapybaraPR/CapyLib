namespace Capy.Core.Database.Models;

/// <summary>
/// Персистентная модель данных игрока (статистика, баланс, настройки).
/// </summary>
public class PlayerDataModel
{
    public string Id { get; set; } = string.Empty; // UserId (e.g. 76561198...@steam)
    public string LastNickname { get; set; } = string.Empty;
    public string LastIp { get; set; } = string.Empty;

    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Wins { get; set; }
    public int RoundsPlayed { get; set; }
    public long TotalPlaytimeSeconds { get; set; }

    public double Balance { get; set; }
    public DateTime FirstJoin { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public Dictionary<string, string> CustomData { get; set; } = new();
}
