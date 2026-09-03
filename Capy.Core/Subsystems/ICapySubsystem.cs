namespace Capy.Core.Subsystems;

/// <summary>
/// Универсальный интерфейс подсистемы ядра CapyLib.
/// Все подсистемы автоматически обнаруживаются и запускаются через рефлексию при подтверждении DRM.
/// </summary>
public interface ICapySubsystem
{
    /// <summary>
    /// Имя подсистемы для логирования.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Приоритет запуска (меньшее число запускается раньше).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Инициализация и запуск подсистемы.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Остановка и освобождение ресурсов подсистемы.
    /// </summary>
    void Shutdown();
}
