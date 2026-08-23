using System.Text.RegularExpressions;
using PlayerRoles;

namespace Capy.Engine.DevTools.Extensions;

public static class RoleTypeIdExtensions {
    public static string TranslatedRoleType(this RoleTypeId roleType, bool useColors = false) {
        string ret = roleType switch {
            RoleTypeId.ClassD => "<color=#ff9933>Сотрудник класса Д</color>",
            RoleTypeId.Scientist => "<color=#e7d573>Научный сотрудник</color>",
            RoleTypeId.FacilityGuard => "<color=#808080>Сотрудник охранны</color>",
            RoleTypeId.NtfPrivate => "<color=#3399ff>Рядовой Мобильной Опер-Группы</color>",
            RoleTypeId.NtfSergeant => "<color=#0066cc>Сержант Мобильной Опер-Группы</color>",
            RoleTypeId.NtfCaptain => "<color=#0047ab>Капитан Мобильной Опер-Группы</color>",
            RoleTypeId.NtfSpecialist => "<color=#003366>Специалист Мобильной Опер-Группы</color>",
            RoleTypeId.Scp049 => "<color=#ff0000>Объект 'Чумной доктор'</color>",
            RoleTypeId.Scp0492 => "<color=#ff0000>Объект 'Зомби доктора'</color>",
            RoleTypeId.Scp096 => "<color=#ff0000>Объект 'Скромник'</color>",
            RoleTypeId.Scp106 => "<color=#ff0000>Объект 'Старик'</color>",
            RoleTypeId.Scp173 => "<color=#ff0000>Объект 'Скульптура'</color>",
            RoleTypeId.Scp939 => "<color=#ff0000>Объект 'Со множеством голосов'</color>",
            RoleTypeId.Scp079 => "<color=#ff0000>Объект 'Старый ИИ'</color>",
            RoleTypeId.ChaosConscript => "<color=#228B22>Солдат повстанцев хаоса</color>",
            RoleTypeId.ChaosRifleman => "<color=#228B22>Стрелок повстанцев хаоса</color>",
            RoleTypeId.ChaosMarauder => "<color=#228B22>Мародёр повстанцев хаоса</color>",
            RoleTypeId.ChaosRepressor => "<color=#228B22>Усмиритель повстанцев хаоса</color>",
            RoleTypeId.Overwatch => "<color=#00bfff>Надзиратель</color>",
            RoleTypeId.Filmmaker => "<color=#000000>Режиссёр</color>",
            RoleTypeId.Scp3114 => "<color=#ff0000>Объект 'Скелет'</color>",
            RoleTypeId.Flamingo => "<color=ff96de>Объект 'Фламинго'</color>",
            RoleTypeId.AlphaFlamingo => "<color=ff1493>Объект 'Альфа фламинго'</color>",
            RoleTypeId.NtfFlamingo => "<color=#3399ff>Фламинго Мобильной Опер-Группы</color>",
            RoleTypeId.ChaosFlamingo => "<color=#228B22>Фламинго повстанцев хаоса</color>",
            RoleTypeId.ZombieFlamingo => "<color=bfff00>Объект 'Зомби фламинго'</color>",
            RoleTypeId.Tutorial => "<color=#ff69b4>Обучение</color>",
            RoleTypeId.Spectator => "<color=#cccccc>Наблюдатель</color>",
            RoleTypeId.None => "<color=#aaaaaa>Никто</color>",
            RoleTypeId.Destroyed => "<color=#aaaaaa>Уничтожен</color>",
            _ => $"{roleType}"
        };
        return useColors ? ret : Regex.Replace(ret, "<[^<>]*>", "");
    }
}