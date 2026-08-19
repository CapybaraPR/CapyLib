using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using PlayerRoles;
using CapyEffect = Capy.Core.Enums.EffectType;
using ExiledEffect = Exiled.API.Enums.EffectType;

namespace Capy.Core.Extensions;

/// <summary>
/// Универсальные методы расширения для перевода любых игровых перечислений (Enums) на русский язык.
/// Поддерживает EffectType, ItemType, RoleTypeId, RoomType, ZoneType, DamageType, Side, AmmoType и любые другие enum'ы.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Получить красивый переведённый русский текст для любого Enum.
    /// Пример: EffectType.AmnesiaItems.Translate() -> "Амнезия (предметы)"
    /// </summary>
    public static string Translate(this Enum value, bool useColors = false)
    {
        if (value == null) return string.Empty;

        // 1. EffectType (CapyLib)
        if (value is CapyEffect capyEffect)
            return capyEffect.TranslateEffectType(useColors);

        // 2. EffectType (Exiled)
        if (value is ExiledEffect exEffect)
            return exEffect.TranslateEffectType(useColors);

        // 3. ItemType
        if (value is ItemType itemType)
            return itemType.TranslateItemType(useColors);

        // 4. RoleTypeId
        if (value is RoleTypeId roleType)
            return roleType.TranslatedRoleType(useColors);

        // 5. Exiled RoomType
        if (value is Exiled.API.Enums.RoomType roomType)
            return TranslateRoomType(roomType, useColors);

        // 6. Exiled ZoneType
        if (value is Exiled.API.Enums.ZoneType zoneType)
            return TranslateZoneType(zoneType, useColors);

        // 7. DamageType
        if (value is Exiled.API.Enums.DamageType exDamage)
            return TranslateDamageType(exDamage, useColors);

        if (value is Capy.Core.Enums.DamageType capyDamage)
            return TranslateCapyDamageType(capyDamage, useColors);

        // 8. Side
        if (value is Exiled.API.Enums.Side side)
            return TranslateSide(side, useColors);

        // 9. AmmoType
        if (value is Exiled.API.Enums.AmmoType ammo)
            return TranslateAmmoType(ammo, useColors);

        // 10. Попытка получить DescriptionAttribute
        FieldInfo? field = value.GetType().GetField(value.ToString());
        if (field != null)
        {
            var descAttr = field.GetCustomAttribute<DescriptionAttribute>();
            if (descAttr != null && !string.IsNullOrWhiteSpace(descAttr.Description))
            {
                return descAttr.Description;
            }
        }

        return value.ToString();
    }

    /// <summary>
    /// Алиас для Translate(useColors)
    /// </summary>
    public static string GetTranslation(this Enum value, bool useColors = false) => value.Translate(useColors);

    /// <summary>
    /// Алиас для Translate(useColors)
    /// </summary>
    public static string ToRussian(this Enum value, bool useColors = false) => value.Translate(useColors);

    /// <summary>
    /// Алиас для Translate(useColors)
    /// </summary>
    public static string GetRussianName(this Enum value, bool useColors = false) => value.Translate(useColors);

    public static string TranslateRoomType(Exiled.API.Enums.RoomType room, bool useColors = false)
    {
        string ret = room switch
        {
            Exiled.API.Enums.RoomType.LczClassDSpawn => "<color=#ff9933>Спавн Класса-D</color>",
            Exiled.API.Enums.RoomType.LczCheckpointA => "<color=#217777>Чекпоинт A (LCZ)</color>",
            Exiled.API.Enums.RoomType.LczCheckpointB => "<color=#217777>Чекпоинт B (LCZ)</color>",
            Exiled.API.Enums.RoomType.LczToilets => "<color=#cccccc>Туалеты (LCZ)</color>",
            Exiled.API.Enums.RoomType.LczArmory => "<color=#887751>Оружейная (LCZ)</color>",
            Exiled.API.Enums.RoomType.LczPlants => "<color=#33cc66>Теплица (LCZ)</color>",
            Exiled.API.Enums.RoomType.LczCafe => "<color=#33cc66>Кафетерий (LCZ)</color>",
            Exiled.API.Enums.RoomType.Lcz173 => "<color=#ff3333>Камера SCP-173</color>",
            Exiled.API.Enums.RoomType.LczGlassBox => "<color=#a3c9db>Камера SCP-372 (Стекло)</color>",
            Exiled.API.Enums.RoomType.Lcz330 => "<color=#212e47>Камера SCP-330 (Конфеты)</color>",
            Exiled.API.Enums.RoomType.Lcz914 => "<color=#deae21>Камера SCP-914</color>",
            Exiled.API.Enums.RoomType.LczAirlock => "<color=#217777>Шлюз (LCZ)</color>",

            Exiled.API.Enums.RoomType.Hcz049 => "<color=#ff0000>Камера SCP-049</color>",
            Exiled.API.Enums.RoomType.Hcz079 => "<color=#ff0000>Камера SCP-079</color>",
            Exiled.API.Enums.RoomType.Hcz106 => "<color=#ff0000>Камера SCP-106</color>",
            Exiled.API.Enums.RoomType.Hcz096 => "<color=#ff0000>Камера SCP-096</color>",
            Exiled.API.Enums.RoomType.Hcz939 => "<color=#ff0000>Камера SCP-939</color>",
            Exiled.API.Enums.RoomType.HczHid => "<color=#206288>Комната MicroHID</color>",
            Exiled.API.Enums.RoomType.HczArmory => "<color=#887751>Оружейная (HCZ)</color>",
            Exiled.API.Enums.RoomType.HczTesla => "<color=#00ffff>Тесла-ворота (HCZ)</color>",
            Exiled.API.Enums.RoomType.HczNuke => "<color=#ff3300>Альфа-боеголовка</color>",
            Exiled.API.Enums.RoomType.HczEzCheckpointA => "<color=#217777>Чекпоинт HCZ-EZ A</color>",
            Exiled.API.Enums.RoomType.HczEzCheckpointB => "<color=#217777>Чекпоинт HCZ-EZ B</color>",
            Exiled.API.Enums.RoomType.HczElevatorA => "<color=#cccccc>Лифт A (HCZ)</color>",
            Exiled.API.Enums.RoomType.HczElevatorB => "<color=#cccccc>Лифт B (HCZ)</color>",
            Exiled.API.Enums.RoomType.HczServerRoom => "<color=#00ccff>Серверная (HCZ)</color>",

            Exiled.API.Enums.RoomType.EzIntercom => "<color=#33ccff>Интерком</color>",
            Exiled.API.Enums.RoomType.EzGateA => "<color=#466fd4>Ворота Gate A</color>",
            Exiled.API.Enums.RoomType.EzGateB => "<color=#466fd4>Ворота Gate B</color>",
            Exiled.API.Enums.RoomType.EzDownstairsPcs => "<color=#00ccff>Нижний офис (EZ)</color>",
            Exiled.API.Enums.RoomType.EzUpstairsPcs => "<color=#00ccff>Верхний офис (EZ)</color>",
            Exiled.API.Enums.RoomType.EzPcs => "<color=#00ccff>Офисы (EZ)</color>",
            Exiled.API.Enums.RoomType.EzCollapsedTunnel => "<color=#888888>Заваленный туннель (EZ)</color>",
            Exiled.API.Enums.RoomType.EzCafeteria => "<color=#ff9966>Кафетерий (EZ)</color>",
            Exiled.API.Enums.RoomType.EzConference => "<color=#3399cc>Конференц-зал (EZ)</color>",
            Exiled.API.Enums.RoomType.EzShelter => "<color=#99cc33>Убежище (EZ)</color>",
            Exiled.API.Enums.RoomType.EzVent => "<color=#777777>Вентиляция (EZ)</color>",

            Exiled.API.Enums.RoomType.Surface => "<color=#33cc33>Поверхность</color>",
            Exiled.API.Enums.RoomType.Pocket => "<color=#225522>Карманное измерение</color>",
            _ => room.ToString()
        };

        return useColors ? ret : Regex.Replace(ret, "<[^<>]*>", "");
    }

    public static string TranslateZoneType(Exiled.API.Enums.ZoneType zone, bool useColors = false)
    {
        string ret = zone switch
        {
            Exiled.API.Enums.ZoneType.LightContainment => "<color=#ffcc00>Лёгкая Зона Содержания</color>",
            Exiled.API.Enums.ZoneType.HeavyContainment => "<color=#cc3300>Тяжёлая Зона Содержания</color>",
            Exiled.API.Enums.ZoneType.Entrance => "<color=#3399cc>Входная Зона</color>",
            Exiled.API.Enums.ZoneType.Surface => "<color=#33cc33>Поверхность</color>",
            Exiled.API.Enums.ZoneType.Other => "<color=#888888>Другое</color>",
            Exiled.API.Enums.ZoneType.Unspecified => "<color=#ffffff>Не определено</color>",
            _ => zone.ToString()
        };

        return useColors ? ret : Regex.Replace(ret, "<[^<>]*>", "");
    }

    public static string TranslateDamageType(Exiled.API.Enums.DamageType damage, bool useColors = false)
    {
        string ret = damage.ToString() switch
        {
            "Falldown" => "<color=#888888>Падение</color>",
            "Warhead" => "<color=#ff3300>Взрыв боеголовки</color>",
            "Decontamination" => "<color=#ffcc00>Обеззараживание</color>",
            "Asphyxiation" => "<color=#5d7b99>Удушье</color>",
            "Bleeding" => "<color=#ff3333>Кровотечение</color>",
            "Poison" => "<color=#00cc44>Отравление</color>",
            "Tesla" => "<color=#00ffff>Тесла-ворота</color>",
            "Scp207" => "<color=#ff3300>Передозировка SCP-207</color>",
            "MicroHid" => "<color=#206288>MicroHID</color>",
            "Explosion" => "<color=#ff6600>Взрыв</color>",
            "Scp018" => "<color=#5a0408>SCP-018 (Мячик)</color>",
            "PocketDimension" => "<color=#225522>Карманное измерение</color>",
            "FriendlyFireDetector" => "<color=#ff0000>Анти-ТК</color>",
            "Crushed" => "<color=#555555>Раздавлен</color>",
            "Disruptor" => "<color=#4c61b5>Разрушитель частиц</color>",
            "Jailbird" => "<color=#93c4c9>Jailbird</color>",
            "CardiacArrest" => "<color=#ff0033>Остановка сердца</color>",
            "Hypothermia" => "<color=#66ccff>Переохлаждение</color>",
            "SeveredHands" => "<color=#990000>Потеря рук</color>",
            _ => damage.ToString()
        };

        return useColors ? ret : Regex.Replace(ret, "<[^<>]*>", "");
    }

    public static string TranslateCapyDamageType(Capy.Core.Enums.DamageType damage, bool useColors = false)
    {
        string ret = damage switch
        {
            Capy.Core.Enums.DamageType.Recontainment => "<color=#00ffff>Повторное сдерживание</color>",
            Capy.Core.Enums.DamageType.Firearm => "<color=#887751>Огнестрельное оружие</color>",
            Capy.Core.Enums.DamageType.Warhead => "<color=#ff3300>Взрыв боеголовки</color>",
            Capy.Core.Enums.DamageType.Universal => "<color=#ffffff>Универсальный</color>",
            Capy.Core.Enums.DamageType.Scp => "<color=#ff0000>Атака SCP</color>",
            Capy.Core.Enums.DamageType.MicroHid => "<color=#206288>MicroHID</color>",
            Capy.Core.Enums.DamageType.Explosion => "<color=#ff6600>Взрыв</color>",
            Capy.Core.Enums.DamageType.Disruptor => "<color=#4c61b5>Разрушитель частиц</color>",
            Capy.Core.Enums.DamageType.Jailbird => "<color=#93c4c9>Jailbird</color>",
            _ => damage.ToString()
        };

        return useColors ? ret : Regex.Replace(ret, "<[^<>]*>", "");
    }

    public static string TranslateSide(Exiled.API.Enums.Side side, bool useColors = false)
    {
        string ret = side switch
        {
            Exiled.API.Enums.Side.Mtf => "<color=#3399ff>Фонд / МОГ</color>",
            Exiled.API.Enums.Side.ChaosInsurgency => "<color=#228B22>Повстанцы Хаоса</color>",
            Exiled.API.Enums.Side.Scp => "<color=#ff0000>Объекты SCP</color>",
            Exiled.API.Enums.Side.Tutorial => "<color=#ff69b4>Обучение</color>",
            Exiled.API.Enums.Side.None => "<color=#cccccc>Никто</color>",
            _ => side.ToString()
        };

        return useColors ? ret : Regex.Replace(ret, "<[^<>]*>", "");
    }

    public static string TranslateAmmoType(Exiled.API.Enums.AmmoType ammo, bool useColors = false)
    {
        string ret = ammo switch
        {
            Exiled.API.Enums.AmmoType.Nato9 => "<color=#cccccc>Патроны 9x19 мм</color>",
            Exiled.API.Enums.AmmoType.Nato556 => "<color=#cccccc>Патроны 5.56x45 мм</color>",
            Exiled.API.Enums.AmmoType.Nato762 => "<color=#cccccc>Патроны 7.62x39 мм</color>",
            Exiled.API.Enums.AmmoType.Ammo12Gauge => "<color=#cccccc>Дробь 12 калибра</color>",
            Exiled.API.Enums.AmmoType.Ammo44Cal => "<color=#cccccc>Патроны .44 Магнум</color>",
            Exiled.API.Enums.AmmoType.None => "<color=#ffffff>Без патронов</color>",
            _ => ammo.ToString()
        };

        return useColors ? ret : Regex.Replace(ret, "<[^<>]*>", "");
    }
}
