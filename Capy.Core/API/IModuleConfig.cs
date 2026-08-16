namespace Capy.Core.API;

/// <summary>
/// Базовый интерфейс для конфигураций модулей CapyLib.
/// </summary>
public interface IModuleConfig
{
    /// <summary>Включен ли режим отладки для данного модуля.</summary>
    bool DebugMode { get; set; }
}
