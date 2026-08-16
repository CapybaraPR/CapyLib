using Capy.Core.Services;

namespace Capy.Core.API;

/// <summary>
/// Абстрактный базовый класс для модулей с типизированной конфигурацией.
/// </summary>
/// <typeparam name="TConfig">Тип конфигурации модуля.</typeparam>
public abstract class BaseModule<TConfig> : ICapyModule where TConfig : class, IModuleConfig, new()
{
    public abstract string Name { get; }
    public virtual string Author => "CapybaraPR";
    public virtual Version Version => new(1, 0, 0);

    public TConfig Config { get; private set; } = new();
    public bool IsEnabled { get; set; } = true;
    public ICapyLogger Log { get; set; } = null!;

    public virtual void OnEnabled() { }
    public virtual void OnDisabled() { }

    public virtual Task OnEnabledAsync()
    {
        OnEnabled();
        return Task.CompletedTask;
    }

    public virtual Task OnDisabledAsync()
    {
        OnDisabled();
        return Task.CompletedTask;
    }

    public Type GetConfigType() => typeof(TConfig);

    public void SetConfig(object config)
    {
        if (config is TConfig typedConfig)
        {
            Config = typedConfig;
            if (Log is CapyLogger capyLogger)
            {
                capyLogger.SetDebug(typedConfig.DebugMode);
            }
        }
    }
}
