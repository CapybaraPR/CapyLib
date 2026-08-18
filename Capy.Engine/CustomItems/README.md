# 🎒 Capy.Engine.CustomItems — Фреймворк кастомных предметов

Модуль **CustomItems** предоставляет удобную и масштабируемую систему регистрации, отслеживания (серийные номера `Serial`), спавна и обработки логики кастомных предметов и оружия.

---

## 🛠️ Базовый класс `CustomItem`

Каждый кастомный предмет наследуется от `Capy.Engine.CustomItems.Base.CustomItem` и переопределяет необходимые свойства и хуки жизненного цикла:

```csharp
using Capy.Engine.CustomItems.Base;
using Capy.Engine.CustomItems.Models;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups;
using UnityEngine;

public sealed class Medigun : CustomItem
{
    public override string Name => "Medigun";
    public override string Description => "Лечебный пистолет, восстанавливающий здоровье союзников при попадании.";
    public override ItemType BaseType => ItemType.GunCOM15;
    public override ItemRarity Rarity => ItemRarity.Rare;
    public override string ColorHex => "#00FF88";

    public override void OnShot(Player player, Item item)
    {
        // Кастомная логика выстрела
        if (Physics.Raycast(player.CameraTransform.position, player.CameraTransform.forward, out var hit, 30f))
        {
            var target = Player.Get(hit.collider);
            if (target != null && target != player && target.IsHuman)
            {
                target.Heal(25f);
                target.ShowHint("<color=#00FF88>Вы были исцелены выстрелом из Medigun!</color>", 3f);
            }
        }
    }
}
```

---

## 📌 Методы жизненного цикла

* `OnPickedUp(Player player, Pickup pickup)` — вызывается при поднятии предмета игроком с земли.
* `OnDropped(Player player, Pickup pickup)` — вызывается при выбрасывании предмета из инвентаря.
* `OnHolding(Player player, Item item)` — вызывается, когда игрок берет предмет в активные руки.
* `OnStopHolding(Player player, Item item)` — вызывается при переключении на другой слот.
* `OnShot(Player player, Item item)` — вызывается при каждом выстреле (для огнестрельного оружия).
* `Give(Player player)` — безопасная выдача предмета игроку с автоматическим внесением в реестр отслеживаемых `TrackedSerials`.
* `Spawn(Vector3 pos, Quaternion rot)` — спавн физического пикапа в игровом мире.
