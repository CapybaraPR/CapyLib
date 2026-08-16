namespace Capy.Engine.NoRules;

/// <summary>
/// Контракт для серверных плагинов режима NoRules.
/// </summary>
public interface ICapyNoRulesPlugin
{
    void ConfigureNoRulesModules();
    void RegisterNoRulesContent();
}

public static class NoRulesDefaults
{
    public static readonly List<string> RecommendedModules = new()
    {
        "HitMarker",
        "ShootingInteractions",
        "Hud",
        "StatsTracker"
    };
}

/// <summary>
/// Базовый класс для создания плагинов сервера NoRules на базе CapyLib.
/// </summary>
public abstract class CapyNoRulesPlugin : Plugin<CapyConfig>, ICapyNoRulesPlugin
{
    public override void OnEnabled()
    {
        base.OnEnabled();

        ConfigureNoRulesModules();
        RegisterNoRulesContent();

        Log.Info($"[NoRules] Плагин '{Name}' успешно инициализирован на базе CapyLib.");
    }

    public abstract void ConfigureNoRulesModules();
    public abstract void RegisterNoRulesContent();
}
