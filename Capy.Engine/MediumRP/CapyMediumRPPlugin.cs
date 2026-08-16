namespace Capy.Engine.MediumRP;

/// <summary>
/// Контракт для серверных плагинов режима MediumRP.
/// </summary>
public interface ICapyMediumRPPlugin
{
    void ConfigureRPModules();
    void RegisterRPContent();
}

public static class MediumRPDefaults
{
    public static readonly List<string> RecommendedModules = new()
    {
        "DisarmedProtection",
        "RemoteKeycard",
        "Talky",
        "Hud"
    };

    public static float DefaultMaxHealth => 100f;
    public static float DefaultHumeShield => 50f;
}

/// <summary>
/// Базовый класс для создания плагинов сервера MediumRP на базе CapyLib.
/// </summary>
public abstract class CapyMediumRPPlugin : Plugin<CapyConfig>, ICapyMediumRPPlugin
{
    public override void OnEnabled()
    {
        base.OnEnabled();

        ConfigureRPModules();
        RegisterRPContent();

        Log.Info($"[MediumRP] Плагин '{Name}' успешно инициализирован на базе CapyLib.");
    }

    public abstract void ConfigureRPModules();
    public abstract void RegisterRPContent();
}
