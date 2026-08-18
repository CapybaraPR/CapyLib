using System.Collections.Generic;
using System.ComponentModel;
using Capy.Core.API;

namespace Capy.API.DiscordBridge;

public sealed class DiscordBridgeConfig : IModuleConfig
{
    [Description("Включить режим отладки для DiscordBridge.")]
    public bool DebugMode { get; set; } = false;

    [Description("Включить HTTP API моста для Discord-бота.")]
    public bool IsEnabled { get; set; } = true;

    [Description("HTTP-префикс API. Для бота на том же сервере оставьте loopback.")]
    public string ListenPrefix { get; set; } = "http://127.0.0.1:8123/";

    public DiscordBridgeConfig()
    {
        try
        {
            if (Exiled.API.Features.Server.Port == 7778)
            {
                ListenPrefix = "http://127.0.0.1:8124/";
            }
        }
        catch
        {
        }
    }

    [Description("Требовать цифровую подпись запросов по SSH / RSA ключу (строгая криптографическая аутентификация).")]
    public bool RequireSshSignature { get; set; } = true;

    [Description("Открытый SSH / RSA ключ бота (формат: ssh-rsa AAAAB3... или PEM). Если указан путь к файлу, ключ загрузится из файла.")]
    public string SshPublicKey { get; set; } = string.Empty;

    [Description("Имя или относительный/абсолютный путь к файлу открытого SSH ключа бота (например aspect_bridge.pub).")]
    public string SshPublicKeyPath { get; set; } = "aspect_bridge.pub";

    [Description("Максимально допустимое расхождение времени запроса (сек) для защиты от атак повторного воспроизведения (Replay Attack).")]
    public int MaxClockDriftSeconds { get; set; } = 60;

    [Description("Общий секретный API-ключ (резервный токен для заголовка X-Aspect-Key).")]
    public string ApiKey { get; set; } = "CHANGE_ME";

    [Description("Разрешённые IP клиентов бота. Пустой список отключает IP-фильтр.")]
    public List<string> AllowedClientIps { get; set; } = new() { "127.0.0.1", "::1" };

    [Description("Публичный адрес SCP:SL для отображения Discord-ботом (IP:порт).")]
    public string ServerAddress { get; set; } = string.Empty;

    [Description("Команды, разрешённые роли с правами RemoteAdmin.")]
    public List<string> RaAllowedCommands { get; set; } = new()
    {
        "broadcast", "bc", "cassie", "players", "list", "kick", "ban", "unban",
        "mute", "unmute", "pm", "warn", "give", "effect", "heal", "hp", "ahp",
        "teleport", "tp", "bring", "goto", "overwatch", "noclip", "god", "bypass",
        "forcestart", "roundrestart",
        "gci", "givecustomitem", "giveitem", "citem", "ciitem",
        "gcr", "givecustomrole", "giverole", "crole", "croles",
        "serpentshand", "shand", "serpents", "shp"
    };

    [Description("Команды, запрещённые даже уровню Ключ Создателя.")]
    public List<string> CreatorBlockedCommands { get; set; } = new();

    [Description("Максимальная длина серверной команды.")]
    public int MaxCommandLength { get; set; } = 512;

    [Description("Максимальное число параллельных HTTP-запросов.")]
    public int MaxConcurrentRequests { get; set; } = 16;

    [Description("Таймаут ожидания ответа игрового потока, секунды.")]
    public int GameThreadTimeoutSeconds { get; set; } = 8;

    [Description("Показывать UserId игроков в ответе API /players.")]
    public bool IncludePlayerUserIds { get; set; } = false;

    [Description("Максимальное число событий, удерживаемых в буфере для Discord-бота.")]
    public int MaxBufferedLogEvents { get; set; } = 2000;

    [Description("Добавлять UserId игроков в события логов Discord.")]
    public bool IncludeLogUserIds { get; set; } = true;

    [Description("Добавлять IP игроков в логи Discord. По умолчанию выключено.")]
    public bool IncludeLogIpAddresses { get; set; } = false;

    [Description("Не раскрывать UserId/IP игроков с включённым Do Not Track.")]
    public bool RespectDoNotTrack { get; set; } = true;

    [Description("UserId администраторов, чьи команды не нужно пересылать в Discord.")]
    public List<string> TrustedAdminUserIds { get; set; } = new();

    [Description("Логировать наказания (кики, баны, муты).")]
    public bool LogPunishments { get; set; } = true;

    [Description("Логировать жизненный цикл раунда и боеголовку.")]
    public bool LogRounds { get; set; } = true;

    [Description("Логировать подключения, отключения, смерти и рестарты сервера.")]
    public bool LogServerEvents { get; set; } = true;

    [Description("Логировать игровые и Discord-команды.")]
    public bool LogCommands { get; set; } = true;

    [Description("Логировать локальные и глобальные репорты.")]
    public bool LogReports { get; set; } = true;

    [Description("Срок действия кода привязки Discord в минутах.")]
    public int LinkCodeLifetimeMinutes { get; set; } = 10;

    [Description("Интервал проверки Discord-ролей подключённых игроков, секунды.")]
    public float DiscordRoleCheckIntervalSeconds { get; set; } = 5f;

    [Description("Снимать управляемую мостом EXILED-группу, если ни одно правило больше не подходит.")]
    public bool DiscordRoleRemoveManagedGroupWhenNoMatch { get; set; } = true;

    [Description("EXILED-группы, которые контроллер Discord-ролей никогда не перезаписывает.")]
    public List<string> DiscordRoleIgnoredGroups { get; set; } = new()
    {
        "owner", "zaowner", "zamowner", "texadmin"
    };

    [Description("Соответствие Discord-ролей EXILED-группам. Поддерживаются operator: or и operator: and.")]
    public List<DiscordRoleMapping> DiscordRoleMappings { get; set; } = new();
}
