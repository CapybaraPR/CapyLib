using Capy.API.Bridges.Models;

namespace Capy.API.Bridges;

/// <summary>
/// Интерфейс интеграции сервера с Discord / Telegram ботами.
/// </summary>
public interface IBotBridge
{
    Task SendServerStatusAsync(ServerStatusDto status);
    Task SendGameEventAsync(GameEventDto gameEvent);
    Task SendPlayerAlertAsync(PlayerInfoDto alert);
}

/// <summary>
/// Интерфейс интеграции сервера с веб-панелью управления.
/// </summary>
public interface IWebBridge
{
    Task<ServerStatusDto> GetServerStatusAsync();
    Task<bool> ExecuteRemoteCommandAsync(string command);
}
