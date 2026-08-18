using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;

namespace Capy.API.DiscordBridge;

internal static class CommandLogFormatter
{
    private static readonly HashSet<string> CustomItemAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "gci", "givecustomitem", "giveitem", "citem", "ciitem"
    };

    private static readonly HashSet<string> CustomRoleAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "gcr", "givecustomrole", "giverole", "crole", "croles"
    };

    private static readonly HashSet<string> SerpentsHandAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "serpentshand", "shand", "serpents", "shp"
    };

    public static List<BridgeLogField> BuildFields(
        string command,
        string response,
        bool success,
        string source,
        string actor,
        DiscordBridgeConfig config)
    {
        var fields = new List<BridgeLogField>
        {
            Field("Источник", source),
            Field("Исполнитель", actor),
            Field("Команда", command),
            Field("Результат", success ? "Успешно" : "Ошибка"),
            Field("Ответ сервера", response)
        };

        AddCustomItemDetails(fields, command, success, config);
        AddCustomRoleDetails(fields, command, success, config);
        AddSerpentsHandDetails(fields, command, config);
        return fields;
    }

    public static string BuildTitle(string command, bool success)
    {
        string normalized = Normalize(command);
        string commandName = normalized.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;
        return CustomItemAliases.Contains(commandName)
            ? success ? "Выдан кастомный предмет" : "Ошибка выдачи кастомного предмета"
            : CustomRoleAliases.Contains(commandName)
                ? success ? "Выдана кастомная роль" : "Ошибка выдачи кастомной роли"
                : SerpentsHandAliases.Contains(commandName)
                    ? success ? "Команда Длани Змея выполнена" : "Ошибка команды Длани Змея"
            : success ? "Команда выполнена" : "Команда завершилась ошибкой";
    }

    private static void AddCustomItemDetails(List<BridgeLogField> fields, string command, bool success, DiscordBridgeConfig config)
    {
        string[] parts = Normalize(command).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !CustomItemAliases.Contains(parts[0]))
            return;

        int argumentIndex = 1;
        if (parts.Length > argumentIndex && (parts[argumentIndex].Equals("give", StringComparison.OrdinalIgnoreCase) ||
                                             parts[argumentIndex].Equals("g", StringComparison.OrdinalIgnoreCase)))
            argumentIndex++;

        if (parts.Length <= argumentIndex)
        {
            fields.Add(Field("Операция", "Список доступных кастомных предметов"));
            return;
        }

        string requested = parts[argumentIndex];
        if (requested.Equals("list", StringComparison.OrdinalIgnoreCase) || requested.Equals("l", StringComparison.OrdinalIgnoreCase))
        {
            fields.Add(Field("Операция", "Список доступных кастомных предметов"));
            return;
        }

        fields.Add(Field("Предмет", requested));
        fields.Add(Field("Операция", success ? "Выдача" : "Попытка выдачи"));

        if (parts.Length > argumentIndex + 1)
            AddTargetFields(fields, parts[argumentIndex + 1], config);
    }

    private static void AddCustomRoleDetails(List<BridgeLogField> fields, string command, bool success, DiscordBridgeConfig config)
    {
        string[] parts = Normalize(command).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !CustomRoleAliases.Contains(parts[0]))
            return;

        int argumentIndex = 1;
        if (parts.Length > argumentIndex && (parts[argumentIndex].Equals("give", StringComparison.OrdinalIgnoreCase) ||
                                             parts[argumentIndex].Equals("g", StringComparison.OrdinalIgnoreCase)))
            argumentIndex++;

        if (parts.Length <= argumentIndex)
        {
            fields.Add(Field("Операция", "Список доступных кастомных ролей"));
            return;
        }

        string requested = parts[argumentIndex];
        if (requested.Equals("list", StringComparison.OrdinalIgnoreCase) || requested.Equals("l", StringComparison.OrdinalIgnoreCase))
        {
            fields.Add(Field("Операция", "Список доступных кастомных ролей"));
            return;
        }

        fields.Add(Field("Роль", requested));
        fields.Add(Field("Операция", success ? "Выдача роли" : "Попытка выдачи роли"));

        if (parts.Length > argumentIndex + 1)
            AddTargetFields(fields, parts[argumentIndex + 1], config);
    }

    private static void AddSerpentsHandDetails(List<BridgeLogField> fields, string command, DiscordBridgeConfig config)
    {
        string[] parts = Normalize(command).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !SerpentsHandAliases.Contains(parts[0]))
            return;

        string sub = parts.Length > 1 ? parts[1].ToLowerInvariant() : "status";
        string action = sub switch
        {
            "spawn" or "s" => "Принудительный спавн отряда",
            "respawn" or "r" => "Респавн отряда",
            "toggle" or "t" => "Переключение спавна Длани Змея",
            "enable" or "on" => "Включение Длани Змея",
            "disable" or "off" => "Отключение Длани Змея",
            "help" or "h" => "Справка по командам Длани Змея",
            _ => "Управление Дланью Змея"
        };
        fields.Add(Field("Действие SH", action));

        if (parts.Length > 2)
            AddTargetFields(fields, parts[2], config);
    }

    private static void AddTargetFields(List<BridgeLogField> fields, string targetSelector, DiscordBridgeConfig config)
    {
        fields.Add(Field("Селектор цели", targetSelector));

        try
        {
            Player? resolved = Player.Get(targetSelector);
            if (resolved != null)
            {
                string targetDesc = $"{resolved.Nickname} (ID: {resolved.Id})";
                if (config.IncludeLogUserIds && (!config.RespectDoNotTrack || !resolved.DoNotTrack))
                    targetDesc += $" [{resolved.UserId}]";
                fields.Add(Field("Игрок-цель", targetDesc));
            }
        }
        catch
        {
        }
    }

    private static string Normalize(string command)
    {
        string trimmed = (command ?? string.Empty).Trim();
        if (trimmed.StartsWith("/", StringComparison.Ordinal) ||
            trimmed.StartsWith(".", StringComparison.Ordinal) ||
            trimmed.StartsWith("!", StringComparison.Ordinal))
        {
            trimmed = trimmed.Substring(1);
        }
        return trimmed.Trim();
    }

    private static BridgeLogField Field(string name, string value, bool inline = true) => new()
    {
        Name = name,
        Value = string.IsNullOrWhiteSpace(value) ? "-" : value,
        Inline = inline
    };
}
