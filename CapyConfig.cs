using System.ComponentModel;
using Exiled.API.Interfaces;

namespace Capy;

/// <summary>
/// Главная конфигурация библиотеки CapyLib.
/// </summary>
public class CapyConfig : IConfig
{
    [Description("Включена ли библиотека CapyLib")]
    public bool IsEnabled { get; set; } = true;

    [Description("Режим подробного логирования (Debug)")]
    public bool Debug { get; set; } = false;

    [Description("Включить обязательную проверку DRM лицензии")]
    public bool ValidateLicense { get; set; } = false;

    [Description("URL сервера валидации лицензий (DRM)")]
    public string LicenseServerUrl { get; set; } = "http://127.0.0.1:5000";

    [Description("Лицензионный ключ сервера (если пусто - читается из license.key)")]
    public string LicenseKey { get; set; } = "DEV_LICENSE";

    [Description("Интервал проверки лицензии в секундах")]
    public float LicenseCheckIntervalSeconds { get; set; } = 300f;

    [Description("Список SteamID разработчиков с доступом к админ-командам управления модулями")]
    public List<string> AuthorizedSteamIds { get; set; } = new()
    {
        "76561198708583029@steam",
        "76561198708583029"
    };

    [Description("Тип базы данных: 'LiteDB' или 'MongoDB'")]
    public string DatabaseType { get; set; } = "LiteDB";

    [Description("Строка подключения к MongoDB (если выбран MongoDB)")]
    public string MongoConnectionString { get; set; } = "mongodb://localhost:27017";

    [Description("Имя базы данных MongoDB")]
    public string MongoDatabaseName { get; set; } = "CapyDb";

    [Description("Включить автоматический сбор и отправку статистики/телеметрии")]
    public bool EnableStatsTracker { get; set; } = true;
}
