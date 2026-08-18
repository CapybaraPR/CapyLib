# 🎭 Capy.Engine.CustomRoles — Фреймворк кастомных ролей и фракций

Модуль **CustomRoles** позволяет создавать уникальные игровые роли, фракции (например, Длань Змеи / Serpents Hand), задавать стартовую экипировку, здоровье, щит Юма (Hume Shield) и уникальные способности.

---

## 🛠️ Создание кастомной роли

Наследуйтесь от `Capy.Engine.CustomRoles.Base.CustomRole` и переопределите необходимые свойства:

```csharp
using Capy.Engine.CustomRoles.Base;
using Exiled.API.Enums;
using PlayerRoles;

public sealed class SpyRole : CustomRole
{
    public override string Name => "Шпион Хаоса";
    public override string Description => "Вы замаскированы под сотрудника МОГ. Ваша цель — саботаж и эвакуация Повстанцев Хаоса.";
    public override RoleTypeId BaseRole => RoleTypeId.NtfSpecialist;

    public override float MaxHealth { get; set; } = 120f;
    public override float HumeShield { get; set; } = 50f;

    public override List<ItemType> StartingItems { get; set; } = new()
    {
        ItemType.GunCrossvec,
        ItemType.KeycardMTFOperative,
        ItemType.ArmorCombat,
        ItemType.Medkit
    };

    public override void OnAssigned(Player player)
    {
        base.OnAssigned(player);

        player.ShowHint($"<color=#0088FF><b>{Name}</b></color>\n{Description}", 10f);
    }
}
```

---

## 📌 Основные функции

* `OnAssigned(Player player)` — вызывается при выдаче роли (устанавливает базовый класс, здоровье, щит и выдает предметы из списка).
* `OnRemoved(Player player)` — вызывается при снятии роли (гибель, респавн, смена класса).
* `IsPlayerRole(Player player)` — быстрая проверка принадлежности игрока к роли по его `UserId`.
* `TrackedPlayers` — хэш-сет с текущими активными носителями роли на сервере.
