# ⚙️ Capy.Engine — Игровой движок и механики SCP:SL

`Capy.Engine` — комплекс игровых механик, низкоуровневой сетевой синхронизации, физических эффектов, кастомного контента (предметы, оружие, роли, фракции), пространственного звука и графического интерфейса для серверов **CapyLib**.

---

## 📁 Структура `Capy.Engine`

```
Capy.Engine/
├── Audio/          # Аудио-система: автозагрузка .ogg файлов и 3D-звук (AudioToggle)
├── Commands/       # Консольные команды игроков (.help, .stats, .top, .staff) и админ-панель (.capy)
├── CustomItems/    # Фреймворк кастомных предметов, оружия и спавн-менеджер
├── CustomRoles/    # Фреймворк кастомных ролей, способностей и фракций (Длань Змеи)
├── Effects/        # Визуальные эффекты (ItemGlowSystem — свечение предметов)
├── FakeSync/       # Сетевая подмена Mirror HLAPI (SyncVar, SyncList, FakeRpc, AdminToy)
├── Hints/          # Движок подсказок и многослойный экранный интерфейс (HUD)
├── MediumRP/       # Специфичные механики режима MediumRP (CapyMediumRPPlugin)
├── NoRules/        # Специфичные механики режима NoRules (CapyNoRulesPlugin)
└── Patches/        # Низкоуровневые Harmony-патчи игрового байт-кода
```

---

## 🛠️ Примеры использования

### 1. Сетевая иллюзия Mirror (`FakeSync`)

Позволяет отправлять персональные сетевые пакеты клиентам, изменяя отображение мира, ролей и объектов без изменения состояния сервера для остальных игроков:

```csharp
using Capy.Engine.FakeSync;
using UnityEngine;

// 1. Подмена роли цели для конкретного наблюдателя (маскировка)
observerPlayer.SendFakeSyncVar(targetPlayer.NetworkIdentity, targetRoleSync, writer =>
{
    writer.WriteRoleType(RoleTypeId.Scientist);
});
```

---

### 2. Создание кастомного предмета (`CustomItems`)

Наследуйтесь от `Capy.Engine.CustomItems.Base.CustomItem` для добавления уникального оружия или артефакта:

```csharp
using Capy.Engine.CustomItems.Base;
using Capy.Engine.CustomItems.Models;
using Exiled.API.Features.Items;
using UnityEngine;

public sealed class Medigun : CustomItem
{
    public override string Name => "Medigun";
    public override string Description => "Исцеляет союзников при выстреле.";
    public override ItemType BaseType => ItemType.GunCOM15;
    public override ItemRarity Rarity => ItemRarity.Rare;
    public override string ColorHex => "#00FF88";

    public override void OnShot(Player player, Item item)
    {
        if (Physics.Raycast(player.CameraTransform.position, player.CameraTransform.forward, out var hit, 25f))
        {
            var target = Player.Get(hit.collider);
            if (target != null && target != player && target.IsHuman)
            {
                target.Heal(20f);
                target.ShowHint("<color=#00FF88>Вы исцелены выстрелом из Medigun!</color>", 2f);
            }
        }
    }
}
```

---

### 3. Создание кастомной роли (`CustomRoles`)

Наследуйтесь от `Capy.Engine.CustomRoles.Base.CustomRole` для создания уникального класса:

```csharp
using Capy.Engine.CustomRoles.Base;
using Exiled.API.Enums;
using PlayerRoles;

public sealed class JuggernautRole : CustomRole
{
    public override string Name => "Джаггернаут Хаоса";
    public override string Description => "Тяжелобронированный боец с повышенным здоровьем и щитом.";
    public override RoleTypeId BaseRole => RoleTypeId.ChaosMarauder;

    public override float MaxHealth { get; set; } = 350f;
    public override float HumeShield { get; set; } = 150f;

    public override List<ItemType> StartingItems { get; set; } = new()
    {
        ItemType.GunLogicer,
        ItemType.ArmorHeavy,
        ItemType.Medkit,
        ItemType.GrenadeHE
    };

    public override void OnAssigned(Player player)
    {
        base.OnAssigned(player);
        player.ShowHint($"<color=#FFA500><b>{Name}</b></color>\n{Description}", 8f);
    }
}
```

---

### 4. Воспроизведение звука (`AudioToggle`)

Автоматическая загрузка `.ogg` файлов из `Plugins/CapyLib/Audio/` и управление воспроизведением:

```csharp
using Capy.Engine.Audio;

// Глобальная музыка конца раунда
AudioToggle.CreateGlobal("round_end_theme", volume: 0.8f);

// 3D-звук, привязанный к позиции игрока
AudioToggle.CreateForPlayer(player, "siren", min: 2f, max: 20f, volume: 1f);

// Остановка воспроизведения
AudioToggle.DestroyGlobal();
```

---

### 5. Экранные подсказки и HUD (`Hints`)

```csharp
using Capy.Engine.Hints;

// Отображение форматированной подсказки
player.ShowCapyHint(
    "<size=28><color=#FFA500>Внимание:</color></size> До прибытия подкрепления осталось <b>30</b> секунд.",
    duration: 5f
);
```
