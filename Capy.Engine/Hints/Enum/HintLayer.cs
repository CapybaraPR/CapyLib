namespace Capy.Engine.Hints.Enum;

/// <summary>
/// Слои приоритета отображения подсказок (Z-Index / Layering).
/// </summary>
public enum HintLayer
{
    /// <summary>Фоновый слой (кастомные визуальные подложки)</summary>
    Background = 0,

    /// <summary>Постоянный пользовательский интерфейс (HUD здоровья, энергии, способностей)</summary>
    Hud = 100,

    /// <summary>Обычные временные уведомления (предметы, чат, хитмаркеры)</summary>
    Notification = 200,

    /// <summary>Важные серверные предупреждения (варны, муты, сообщения админов)</summary>
    Alert = 300,

    /// <summary>Критический экран (полный запрет или важнейшие спавн-титры)</summary>
    Critical = 400
}
