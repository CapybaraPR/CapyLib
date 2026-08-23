using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Capy.Engine.Studio.Core;
using Capy.Engine.Studio.ToolGun;
using CommandSystem;
using Exiled.API.Features;
using UnityEngine;

namespace Capy.Engine.Studio.Commands;

/// <summary>
/// Главная команда строителя (ToolGun).
/// </summary>
[CommandHandler(typeof(ClientCommandHandler))]
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class ToolGunCommand : ICommand
{
    public string Command => "toolgun";
    public string[] Aliases => new[] { "tg", "capytool", "ctool" };
    public string Description => "Включает или выключает интерактивный режим строителя (CapyStudio ToolGun & PhysGun).";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player == null)
        {
            response = "Команда доступна только игрокам на сервере.";
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
    public string Command => "schem";
    public string[] Aliases => new[] { "schematic", "cschem", "capyschem" };
    public string Description => "Управление схематиками (спавн, список, слияние, очистка, перезагрузка).";

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
            response = "\n<color=#38bdf8><b>🏗️ === [ CapyStudio Schematics ] ===</b></color>\n" +
                       "<color=#a3e635>• .schem spawn <имя> [масштаб]</color> -- Заспавнить схематику перед собой\n" +
                       "<color=#a3e635>• .schem list</color> -- Показать доступные схематики\n" +
                       "<color=#a3e635>• .schem merge <схем1> <схем2> <итог></color> -- Объединить две схематики в одну\n" +
                       "<color=#a3e635>• .schem clear</color> -- Удалить все активные схематики\n" +
                       "<color=#a3e635>• .schem reload</color> -- Очистить кэш и перечитать файлы с диска";
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
                    response = "<color=red>[ОШИБКА]</color> Использование: <b>.schem spawn <имя_схематики> [масштаб]</b>";
                    return false;
                }

                if (player == null)
                {
                    response = "Спавн по прицелу доступен только игрокам в игре.";
                    return false;
                }

                string name = arguments.At(1);
                float scale = 1.0f;
                if (arguments.Count >= 3 && float.TryParse(arguments.At(2), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedScale))
                {
                    scale = Mathf.Clamp(parsedScale, 0.05f, 50.0f);
                }

                Vector3 targetPos = player.Position + (player.Rotation * (Vector3.forward * 3f)) + Vector3.up * 0.1f;
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

            case "merge":
            {
                if (arguments.Count < 4)
                {
                    response = "<color=red>[ОШИБКА]</color> Использование: <b>.schem merge <схематика1> <схематика2> <новое_имя></b>";
                    return false;
                }

                string s1 = arguments.At(1);
                string s2 = arguments.At(2);
                string output = arguments.At(3);

                if (SchematicLoader.Merge(s1, s2, output, out string savedPath))
                {
                    response = $"<color=green>[СЛИЯНИЕ]</color> Схематики <b>{s1}</b> и <b>{s2}</b> успешно объединены в <b>{output}.json</b>!";
                    return true;
                }

                response = "<color=red>[ОШИБКА]</color> Не удалось объединить схематики. Проверьте правильность имён исходных файлов.";
                return false;
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

                string mapName = arguments.At(1);
                if (MapManager.LoadMap(mapName, out string loadResponse))
                {
                    response = loadResponse;
                    return true;
                }

                response = loadResponse;
                return false;
            }

            case "clear":
            case "unload":
            {
                MapManager.ClearCurrentMap();
                response = "<color=yellow>[КАРТЫ]</color> Текущая карта выгружена, все объекты очищены.";
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

/// <summary>
/// Команда живого захвата и перемещения объектов (PhysGun Grab).
/// </summary>
[CommandHandler(typeof(ClientCommandHandler))]
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class GrabCommand : ICommand
{
    public string Command => "grab";
    public string[] Aliases => new[] { "cgrab", "physgun" };
    public string Description => "Захватывает объект перед прицелом и перемещает его в реальном времени.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player == null)
        {
            response = "Команда доступна только игрокам.";
            return false;
        }

        if (!player.RemoteAdminAccess)
        {
            response = "<color=red>[ДОСТУП ЗАПРЕЩЁН]</color> Требуются права администратора.";
            return false;
        }

        if (!CapyToolGun.IsHoldingToolGun(player))
        {
            CapyToolGun.ToggleToolGun(player, out _);
        }

        response = "<color=green>[PHYSGUN]</color> Наведите COM-15 на объект и нажмите <b>ПКМ</b> для захвата!";
        return true;
    }
}
