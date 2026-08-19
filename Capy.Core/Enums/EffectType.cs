using System;
using System.ComponentModel;

namespace Capy.Core.Enums;

public enum EffectType
{
    [Description("Отсутствует")]
    None,

    [Description("Амнезия (предметы)")]
    AmnesiaItems,

    [Description("Амнезия (зрение)")]
    AmnesiaVision,

    [Description("Удушье")]
    Asphyxiated,

    [Description("Кровотечение")]
    Bleeding,

    [Description("Ослепление")]
    Blinded,

    [Description("Ожог")]
    Burned,

    [Description("Контузия")]
    Concussed,

    [Description("Коррозия")]
    Corroding,

    [Description("Глухота")]
    Deafened,

    [Description("Обеззараживание")]
    Decontaminating,

    [Description("Травма ноги")]
    Disabled,

    [Description("Опутывание")]
    Ensnared,

    [Description("Истощение")]
    Exhausted,

    [Description("Вспышка")]
    Flashed,

    [Description("Кровоизлияние")]
    Hemorrhage,

    [Description("Воодушевление")]
    Invigorated,

    [Description("Снижение урона в тело")]
    BodyshotReduction,

    [Description("Отравление")]
    Poisoned,

    [Description("SCP-207 (Кола)")]
    Scp207,

    [Description("Невидимость")]
    Invisible,

    [Description("Воронка")]
    SinkHole,

    [Description("Снижение урона")]
    DamageReduction,

    [Description("Ускорение")]
    MovementBoost,

    [Description("Радужный вкус")]
    RainbowTaste,

    [Description("Оторванные руки")]
    SeveredHands,

    [Description("Загрязнение")]
    Stained,

    [Description("Живучесть")]
    Vitality,

    [Description("Переохлаждение")]
    Hypothermia,

    [Description("SCP-1853 (Допинг)")]
    Scp1853,

    [Description("Остановка сердца")]
    CardiacArrest,

    [Description("Недостаток света")]
    InsufficientLighting,

    [Description("Заглушение музыки")]
    SoundtrackMute,

    [Description("Защита возрождения")]
    SpawnProtected,

    [Description("Травмирован")]
    Traumatized,

    [Description("Анти-SCP-207")]
    AntiScp207,

    [Description("Просканирован")]
    Scanned,

    [Description("Карманная коррозия")]
    PocketCorroding,

    [Description("Бесшумный шаг")]
    SilentWalk,

    [Obsolete("Not functional in-game")]
    [Description("Зефир")]
    Marshmallow,

    [Description("Удушение")]
    Strangled,

    [Description("Призрак")]
    Ghostly,

    [Description("Контроль тумана")]
    FogControl,

    [Description("Замедление")]
    Slowness,

    [Description("SCP-1344 (Очки)")]
    Scp1344,

    [Description("Выколотые глаза")]
    SeveredEyes,

    [Description("Падение в бездну")]
    PitDeath,

    [Description("Размытие")]
    Blurred,

    [Obsolete("Only available for Christmas and AprilFools.")]
    [Description("Превращение в фламинго")]
    BecomingFlamingo,

    [Obsolete("Only available for Christmas and AprilFools.")]
    [Description("SCP-559 (Торт)")]
    Scp559,

    [Obsolete("Only available for Christmas and AprilFools.")]
    [Description("Цель SCP-956 (Пиньята)")]
    Scp956Target,

    [Obsolete("Only available for Christmas and AprilFools.")]
    [Description("Заснежен")]
    Snowed,

    [Description("Обнаружен SCP-1344")]
    Scp1344Detected,

    [Description("SCP-1576 (Граммофон)")]
    Scp1576,

    [Description("Легковес")]
    Lightweight,

    [Description("Тяжёлая поступь")]
    HeavyFooted,

    [Description("Угасание")]
    Fade,

    [Description("Ночное зрение")]
    NightVision,

    [Description("Металл")]
    Metal,

    [Description("Оранжевая конфета")]
    OrangeCandy,

    [Description("Свидетель оранжевой конфеты")]
    OrangeWitness,

    [Description("Призматический")]
    Prismatic,

    [Description("Замедленный метаболизм")]
    SlowMetabolism,

    [Description("Острый")]
    Spicy,

    [Description("Тяга к сахару")]
    SugarCrave,

    [Description("Сахарный кайф")]
    SugarHigh,

    [Description("Сахарный раш")]
    SugarRush,

    [Description("Временный обход")]
    TemporaryBypass,

    [Description("Травмирован злом")]
    TraumatizedByEvil,

    [Description("Белая конфета")]
    WhiteCandy,

    [Description("Воскрешён SCP-1509")]
    Scp1509Resurrected,

    [Description("Фокусированное зрение")]
    FocusedVision,

    [Description("Аномальная регенерация")]
    AnomalousRegeneration,

    [Description("Аномальная цель")]
    AnomalousTarget
}
