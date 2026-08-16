namespace Capy.Core.API;

/// <summary>
/// Базовый контракт для всех модулей и подсистем CapyLib.
/// </summary>
public interface ICapyModule
{
    /// <summary>Уникальное имя модуля.</summary>
    string Name { get; }

    /// <summary>Автор модуля.</summary>
    string Author { get; }

    /// <summary>Версия модуля.</summary>
    Version Version { get; }

    /// <summary>Текущий статус активности модуля.</summary>
    bool IsEnabled { get; set; }

    /// <summary>Экземпляр логгера модуля.</summary>
    ICapyLogger Log { get; set; }

    /// <summary>Синхронный запуск модуля.</summary>
    void OnEnabled();

    /// <summary>Синхронная остановка модуля.</summary>
    void OnDisabled();

    /// <summary>Асинхронный запуск модуля.</summary>
    Task OnEnabledAsync();

    /// <summary>Асинхронная остановка модуля.</summary>
    Task OnDisabledAsync();

    /// <summary>Получение типа конфигурации модуля.</summary>
    Type GetConfigType();

    /// <summary>Применение десериализованной конфигурации.</summary>
    void SetConfig(object config);
}
