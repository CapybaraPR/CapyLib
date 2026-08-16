namespace Capy.API.Bridges.Models;

public class ServerStatusDto
{
    public string ServerName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public double Tps { get; set; }
    public bool RoundInProgress { get; set; }
    public string RoundDuration { get; set; } = "00:00";
}

public class PlayerInfoDto
{
    public string UserId { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public bool IsAlive { get; set; }
    public int Ping { get; set; }
}

public class GameEventDto
{
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Data { get; set; } = new();
}
