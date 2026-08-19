using System;
using System.Text.RegularExpressions;
using CapyEffect = Capy.Core.Enums.EffectType;
using ExiledEffect = Exiled.API.Enums.EffectType;

namespace Capy.Core.Extensions;

public static class EffectTypeExtensions
{
    public static string TranslateEffectType(this CapyEffect effect, bool useColors = false)
    {
        string ret = effect switch
        {
            CapyEffect.None => "<color=#ffffff>Отсутствует</color>",
            CapyEffect.AmnesiaItems => "<color=#c46a3f>Амнезия (предметы)</color>",
            CapyEffect.AmnesiaVision => "<color=#c46a3f>Амнезия (зрение)</color>",
            CapyEffect.Asphyxiated => "<color=#5d7b99>Удушье</color>",
            CapyEffect.Bleeding => "<color=#ff3333>Кровотечение</color>",
            CapyEffect.Blinded => "<color=#cccccc>Ослепление</color>",
            CapyEffect.Burned => "<color=#ff6600>Ожог</color>",
            CapyEffect.Concussed => "<color=#9966cc>Контузия</color>",
            CapyEffect.Corroding => "<color=#339933>Коррозия</color>",
            CapyEffect.Deafened => "<color=#999999>Глухота</color>",
            CapyEffect.Decontaminating => "<color=#ffcc00>Обеззараживание</color>",
            CapyEffect.Disabled => "<color=#cc3333>Травма ноги</color>",
            CapyEffect.Ensnared => "<color=#666633>Опутывание</color>",
            CapyEffect.Exhausted => "<color=#888888>Истощение</color>",
            CapyEffect.Flashed => "<color=#ffffff>Вспышка</color>",
            CapyEffect.Hemorrhage => "<color=#cc0000>Кровоизлияние</color>",
            CapyEffect.Invigorated => "<color=#33ccff>Воодушевление</color>",
            CapyEffect.BodyshotReduction => "<color=#6699ff>Снижение урона в тело</color>",
            CapyEffect.Poisoned => "<color=#00cc44>Отравление</color>",
            CapyEffect.Scp207 => "<color=#ff3300>SCP-207 (Кола)</color>",
            CapyEffect.Invisible => "<color=#a3c9db>Невидимость</color>",
            CapyEffect.SinkHole => "<color=#222222>Воронка</color>",
            CapyEffect.DamageReduction => "<color=#3366cc>Снижение урона</color>",
            CapyEffect.MovementBoost => "<color=#00ffcc>Ускорение</color>",
            CapyEffect.RainbowTaste => "<color=#ff66cc>Радужный вкус</color>",
            CapyEffect.SeveredHands => "<color=#990000>Оторванные руки</color>",
            CapyEffect.Stained => "<color=#666600>Загрязнение</color>",
            CapyEffect.Vitality => "<color=#00ff66>Живучесть</color>",
            CapyEffect.Hypothermia => "<color=#66ccff>Переохлаждение</color>",
            CapyEffect.Scp1853 => "<color=#339966>SCP-1853 (Допинг)</color>",
            CapyEffect.CardiacArrest => "<color=#ff0033>Остановка сердца</color>",
            CapyEffect.InsufficientLighting => "<color=#333333>Недостаток света</color>",
            CapyEffect.SoundtrackMute => "<color=#777777>Заглушение музыки</color>",
            CapyEffect.SpawnProtected => "<color=#ffff00>Защита возрождения</color>",
            CapyEffect.Traumatized => "<color=#993333>Травмирован</color>",
            CapyEffect.AntiScp207 => "<color=#ff6699>Анти-SCP-207</color>",
            CapyEffect.Scanned => "<color=#33ffff>Просканирован</color>",
            CapyEffect.PocketCorroding => "<color=#225522>Карманная коррозия</color>",
            CapyEffect.SilentWalk => "<color=#99ccff>Бесшумный шаг</color>",
            CapyEffect.Marshmallow => "<color=#ffffff>Зефир</color>",
            CapyEffect.Strangled => "<color=#660033>Удушение</color>",
            CapyEffect.Ghostly => "<color=#ccccff>Призрак</color>",
            CapyEffect.FogControl => "<color=#999999>Контроль тумана</color>",
            CapyEffect.Slowness => "<color=#999966>Замедление</color>",
            CapyEffect.Scp1344 => "<color=#ff9900>SCP-1344 (Очки)</color>",
            CapyEffect.SeveredEyes => "<color=#880000>Выколотые глаза</color>",
            CapyEffect.PitDeath => "<color=#111111>Падение в бездну</color>",
            CapyEffect.Blurred => "<color=#bbbbbb>Размытие</color>",
            CapyEffect.BecomingFlamingo => "<color=#ff96de>Превращение в фламинго</color>",
            CapyEffect.Scp559 => "<color=#ff99cc>SCP-559 (Торт)</color>",
            CapyEffect.Scp956Target => "<color=#ff3366>Цель SCP-956 (Пиньята)</color>",
            CapyEffect.Snowed => "<color=#e0f7fa>Заснежен</color>",
            CapyEffect.Scp1344Detected => "<color=#ffaa00>Обнаружен SCP-1344</color>",
            CapyEffect.Scp1576 => "<color=#c1b073>SCP-1576 (Граммофон)</color>",
            CapyEffect.Lightweight => "<color=#80deea>Легковес</color>",
            CapyEffect.HeavyFooted => "<color=#78909c>Тяжёлая поступь</color>",
            CapyEffect.Fade => "<color=#b0bec5>Угасание</color>",
            CapyEffect.NightVision => "<color=#76ff03>Ночное зрение</color>",
            CapyEffect.Metal => "<color=#90a4ae>Металл</color>",
            CapyEffect.OrangeCandy => "<color=#ff9800>Оранжевая конфета</color>",
            CapyEffect.OrangeWitness => "<color=#ffa726>Свидетель оранжевой конфеты</color>",
            CapyEffect.Prismatic => "<color=#ea80fc>Призматический</color>",
            CapyEffect.SlowMetabolism => "<color=#8d6e63>Замедленный метаболизм</color>",
            CapyEffect.Spicy => "<color=#ff5722>Острый</color>",
            CapyEffect.SugarCrave => "<color=#e91e63>Тяга к сахару</color>",
            CapyEffect.SugarHigh => "<color=#f06292>Сахарный кайф</color>",
            CapyEffect.SugarRush => "<color=#f48fb1>Сахарный раш</color>",
            CapyEffect.TemporaryBypass => "<color=#29b6f6>Временный обход</color>",
            CapyEffect.TraumatizedByEvil => "<color=#d32f2f>Травмирован злом</color>",
            CapyEffect.WhiteCandy => "<color=#ffffff>Белая конфета</color>",
            CapyEffect.Scp1509Resurrected => "<color=#ab47bc>Воскрешён SCP-1509</color>",
            CapyEffect.FocusedVision => "<color=#00e5ff>Фокусированное зрение</color>",
            CapyEffect.AnomalousRegeneration => "<color=#00e676>Аномальная регенерация</color>",
            CapyEffect.AnomalousTarget => "<color=#ff1744>Аномальная цель</color>",
            _ => effect.ToString()
        };

        return useColors ? ret : Regex.Replace(ret, "<[^<>]*>", "");
    }

    public static string Translate(this CapyEffect effect, bool useColors = false) => effect.TranslateEffectType(useColors);
    public static string GetTranslation(this CapyEffect effect, bool useColors = false) => effect.TranslateEffectType(useColors);
    public static string ToRussian(this CapyEffect effect, bool useColors = false) => effect.TranslateEffectType(useColors);
    public static string GetRussianName(this CapyEffect effect, bool useColors = false) => effect.TranslateEffectType(useColors);

    public static string TranslateEffectType(this ExiledEffect effect, bool useColors = false)
    {
        if (Enum.TryParse<CapyEffect>(effect.ToString(), true, out var capyEffect))
        {
            return capyEffect.TranslateEffectType(useColors);
        }
        return effect.ToString();
    }

    public static string Translate(this ExiledEffect effect, bool useColors = false) => effect.TranslateEffectType(useColors);
    public static string GetTranslation(this ExiledEffect effect, bool useColors = false) => effect.TranslateEffectType(useColors);
    public static string ToRussian(this ExiledEffect effect, bool useColors = false) => effect.TranslateEffectType(useColors);
    public static string GetRussianName(this ExiledEffect effect, bool useColors = false) => effect.TranslateEffectType(useColors);
}
