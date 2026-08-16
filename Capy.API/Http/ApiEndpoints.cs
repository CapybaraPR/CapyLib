namespace Capy.API.Http;

/// <summary>
/// Конфигурация URL эндпоинтов внешнего API.
/// </summary>
public static class ApiEndpoints
{
    public static string BaseUrl { get; set; } = "http://127.0.0.1:5000/api/v1";

    public static string AuthLog => BaseUrl.TrimEnd('/') + "/auth-log";
    public static string StatsUpdate => BaseUrl.TrimEnd('/') + "/stats/update";
    public static string ServerStatus => BaseUrl.TrimEnd('/') + "/server/status";
    public static string Webhooks => BaseUrl.TrimEnd('/') + "/events";
}
