using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CommandSystem;
using Capy.Engine.Studio.Core;
using Capy.Engine.Studio.ToolGun;
using Exiled.API.Features;
using UnityEngine;

namespace Capy.Engine.Studio.Commands;

/// <summary>
/// Команда ToolGun для входа в режим строителя.
/// </summary>
[CommandHandler(typeof(ClientCommandHandler))]
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class ToolGunCommand : ICommand
{
    public string Command => "toolgun";
    public string[] Aliases => new[] { "tg", "builder", "build" };
    public string Description => "Включить / выключить режим строительного инструмента ToolGun.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player == null)
        {
            response = "<color=red>[ОШИБКА]</color> Команда доступна только игрокам на сервере.";
            return false;
        }

        if (!player.RemoteAdminAccess)
        {
            response = "<color=red>[ДОСТУП ЗАПРЕЩЁН]</color> Требуются права администратора.";
            return false;
        }

        return CapyToolGun.ToggleToolGun(player, out response);
    }
}

/// <summary>
/// Команда управления схематиками CapyStudio.
/// </summary>
[CommandHandler(typeof(ClientCommandHandler))]
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SchematicCommand : ICommand
{
    public string Command => "schematic";
    public string[] Aliases => new[] { "schem", "cschem" };
    public string Description => "Управление схематиками CapyStudio (спавн, список, очистка).";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player == null)
        {
            response = "<color=red>[ОШИБКА]</color> Команда доступна только игрокам на сервере.";
            return false;
        }

        if (!player.RemoteAdminAccess)
        {
            response = "<color=red>[ДОСТУП ЗАПРЕЩЁН]</color> Требуются права администратора.";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = "\n<color=#38bdf8><b>🏗️ === [ CapyStudio Schematics ] ===</b></color>\n" +
                       "<color=#a3e635>• .schem spawn <имя> [масштаб]</color> -- Заспавнить схематику в точке прицела\n" +
                       "<color=#a3e635>• .schem list</color> -- Список всех доступных .json схематик\n" +
                       "<color=#a3e635>• .schem clear</color> -- Удалить все заспавненные схематики\n" +
                       "<color=#a3e635>• .toolgun (.tg)</color> -- Активировать строительный ToolGun";
            return true;
        }

        string subCmd = arguments.At(0).ToLowerInvariant();

        switch (subCmd)
        {
            case "spawn":
            case "s":
            {
                if (arguments.Count < 2)
                {
                    response = "<color=red>[ОШИБКА]</color> Использование: <b>.schem spawn <название> [масштаб]</b>";
                    return false;
                }

                string name = arguments.At(1);
                float scale = 1.0f;
                if (arguments.Count > 2 && float.TryParse(arguments.At(2), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                {
                    scale = Mathf.Clamp(parsed, 0.05f, 20f);
                }

                Vector3 targetPos = player.Position + (player.Rotation * Vector3.forward * 2.0f);
                Quaternion targetRot = player.Rotation;

                if (Physics.Raycast(player.CameraTransform.position, player.CameraTransform.forward, out var hit, 60f))
                {
                    targetPos = hit.point;
                    targetRot = Quaternion.LookRotation(hit.normal == Vector3.up ? player.CameraTransform.forward : hit.normal);
                }

                var spawned = SchematicLoader.Spawn(name, targetPos, targetRot, Vector3.one * scale);
                if (spawned == null)
                {
                    response = $"<color=red>[ОШИБКА]</color> Не удалось заспавнить схематику '<b>{name}</b>'. Проверьте наличие файла .json.";
                    return false;
                }

                response = $"<color=green>[СХЕМАТИКА]</color> Схематика <b>{name}</b> успешно заспавнена (блоков: {spawned.Data.Blocks.Count}, масштаб: {scale:F2})!";
                return true;
            }

            case "list":
            case "l":
            {
                var list = SchematicLoader.GetAvailableSchematics();
                if (list.Count == 0)
                {
                    response = "<color=yellow>[СХЕМАТИКИ]</color> Папки Schematics/ пусты. Поместите .json файлы в Configs/CapyLib/Schematics/";
                    return true;
                }

                string formattedList = string.Join("\n", list.Select(s => $"  <color=#67e8f9>•</color> <color=#ffffff>{s}</color>"));
                response = $"\n<color=#38bdf8><b>Доступные схематики ({list.Count}):</b></color>\n{formattedList}";
                return true;
            }

            case "clear":
            case "destroyall":
            {
                int count = SchematicLoader.SpawnedSchematics.Count;
                SchematicLoader.DestroyAll();
                response = $"<color=yellow>[СХЕМАТИКИ]</color> Все активные схематики (<b>{count}</b> шт.) удалены.";
                return true;
            }

            case "reload":
            case "r":
            {
                SchematicLoader.ClearCache();
                response = "<color=green>[СХЕМАТИКИ]</color> Кэш схематик очищен. Все файлы .json будут прочитаны заново с диска!";
                return true;
            }

            default:
            {
                response = $"<color=red>[ОШИБКА]</color> Неизвестная подкоманда '{subCmd}'. Введите <b>.schem</b> для справки.";
                return false;
            }
        }
    }
}

/// <summary>
/// Команда управления картами CapyStudio.
/// </summary>
[CommandHandler(typeof(ClientCommandHandler))]
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class MapCommand : ICommand
{
    public string Command => "map";
    public string[] Aliases => new[] { "cmap", "capymap" };
    public string Description => "Управление картами CapyStudio (загрузка, очистка).";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player != null && !player.RemoteAdminAccess)
        {
            response = "<color=red>[ДОСТУП ЗАПРЕЩЁН]</color> Требуются права администратора.";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = "\n<color=#38bdf8><b>🗺️ === [ CapyStudio Maps ] ===</b></color>\n" +
                       "<color=#a3e635>• .map load <имя></color> -- Загрузить карту из Configs/CapyLib/Maps/\n" +
                       "<color=#a3e635>• .map clear</color> -- Очистить текущую карту\n" +
                       "<color=#a3e635>• .map list</color> -- Список сохранённых карт";
            return true;
        }

        string subCmd = arguments.At(0).ToLowerInvariant();

        switch (subCmd)
        {
            case "load":
            {
                if (arguments.Count < 2)
                {
                    response = "<color=red>[ОШИБКА]</color> Использование: <b>.map load <имя_карты></b>";
                    return false;
                }

                return MapManager.LoadMap(arguments.At(1), out response);
            }

            case "clear":
            {
                int count = MapManager.ClearCurrentMap();
                response = $"<color=yellow>[КАРТА]</color> Карта очищена. Удалено объектов: <b>{count}</b>.";
                return true;
            }

            case "list":
            {
                if (!Directory.Exists(MapManager.MapsDirectoryPath))
                {
                    response = "<color=yellow>[КАРТЫ]</color> Папка Maps/ пуста.";
                    return true;
                }

                var files = Directory.GetFiles(MapManager.MapsDirectoryPath, "*.json");
                if (files.Length == 0)
                {
                    response = "<color=yellow>[КАРТЫ]</color> Нет сохранённых файлов карт в Maps/";
                    return true;
                }

                string listStr = string.Join("\n", files.Select(f => $"  <color=#67e8f9>•</color> <color=#ffffff>{Path.GetFileNameWithoutExtension(f)}</color>"));
                response = $"\n<color=#38bdf8><b>Сохранённые карты ({files.Length}):</b></color>\n{listStr}";
                return true;
            }

            default:
            {
                response = $"<color=red>[ОШИБКА]</color> Неизвестная подкоманда '{subCmd}'. Введите <b>.map</b> для справки.";
                return false;
            }
        }
    }
}
