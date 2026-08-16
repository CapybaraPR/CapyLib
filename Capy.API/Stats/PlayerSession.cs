namespace Capy.API.Stats;

/// <summary>
/// In-memory сессия игрока для сбора игровой статистики и аудита.
/// </summary>
public class PlayerSession
{
    public string UserId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTime JoinTime { get; set; } = DateTime.UtcNow;

    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int RoundsPlayed { get; set; }

    public int SessionDurationSeconds => (int)(DateTime.UtcNow - JoinTime).TotalSeconds;
}
