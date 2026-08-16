using Capy.Core.API;

namespace Capy.API.Stats;

/// <summary>
/// Конфигурация модуля трекинга статистики и телеметрии.
/// </summary>
public class StatsConfig : IModuleConfig
{
    public bool DebugMode { get; set; } = false;
    public bool EnableAuthLogging { get; set; } = true;
    public bool EnableStatsTracking { get; set; } = true;
    public string ApiUrl { get; set; } = "http://127.0.0.1:5000/api/v1";
}
