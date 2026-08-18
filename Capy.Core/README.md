# 🧠 Capy.Core — Ядро и архитектурная основа фреймворка

`Capy.Core` — фундаментальный уровень библиотеки **CapyLib**, предоставляющий управление жизненным циклом компонентов, доступ к базам данных, систему лицензирования (DRM), контейнер зависимостей (DI/IoC), типизированную шину событий, расширения стандартных классов SCP:SL/Unity и вспомогательные сервисы.

---

## 📁 Структура `Capy.Core`

```
Capy.Core/
├── API/            # Базовые классы (BaseModule<TConfig>, ICapyModule, IModuleConfig)
│   └── Attributes/ # Атрибуты зависимостей ([DependsOn])
├── Database/       # Провайдеры БД (LiteDB, MongoDB, InMemory) и модели данных
├── DRM/            # Менеджер лицензий (HWID, проверка RSA-подписи, анти-тампер)
├── Enums/          # Расширенные игровые перечисления (DamageType, EffectType, Colors)
├── Extensions/     # Методы расширения (Player, Network, Damage, Reflection, Dummy)
├── Features/       # Готовые фичи (Global/Player Cooldown, PrefabManager, Helpers)
├── Interfaces/     # Системные интерфейсы фреймворка
├── Loader/         # Модульный загрузчик (топологическая сортировка графа DAG)
└── Services/       # Сервисы (EventBus, ServiceContainer, PlaceholderReplacer)
```

---

## 🛠️ Примеры использования

### 1. Создание собственного модуля с зависимостями

Все модули наследуются от `BaseModule<TConfig>` и автоматически регистрируются в системе:

```csharp
using Capy.Core.API;
using Capy.Core.API.Attributes;

// Указываем зависимости: модуль запустится только после DatabaseModule
[DependsOn(typeof(DatabaseModule))]
public sealed class MyFeatureModule : BaseModule<MyFeatureConfig>
{
    public override string Name => "MyFeature";
    public override string Author => "CapybaraPR";
    public override Version Version => new(1, 0, 0);

    public override void OnEnabled()
    {
        Log.Info($"Модуль MyFeature успешно запущен с параметром: {Config.SomeSetting}");
    }

    public override void OnDisabled()
    {
        Log.Info("Модуль MyFeature остановлен.");
    }
}
```

---

### 2. Работа с базой данных (LiteDB / MongoDB)

Универсальный доступ к коллекциям данных игроков и сервера:

```csharp
using Capy.Core.Database;
using Capy.Core.Database.Models;

// Сохранение или обновление данных игрока
public void SavePlayerData(Player player, int pointsToAdd)
{
    var db = DatabaseModule.DatabaseProvider;
    var collection = db.GetCollection<UserAccount>("players");

    var account = collection.FindById(player.UserId) ?? new UserAccount 
    { 
        UserId = player.UserId,
        Nickname = player.Nickname 
    };

    account.Points += pointsToAdd;
    account.LastSeen = DateTime.UtcNow;

    collection.Upsert(account);
}
```

---

### 3. Использование шины событий (`EventBus`)

Слабосвязанный обмен событиями между изолированными модулями:

```csharp
using Capy.Core.Services;

// 1. Определение типа события
public readonly struct PlayerPurchasedItemEvent
{
    public Player Buyer { get; }
    public string ItemName { get; }

    public PlayerPurchasedItemEvent(Player buyer, string itemName)
    {
        Buyer = buyer;
        ItemName = itemName;
    }
}

// 2. Подписка на событие в любом модуле
EventBus.Subscribe<PlayerPurchasedItemEvent>(ev =>
{
    ev.Buyer.ShowHint($"Вы успешно приобрели: <color=#00FF88>{ev.ItemName}</color>!", 3f);
});

// 3. Публикация события
EventBus.Publish(new PlayerPurchasedItemEvent(player, "MicroHID"));
```

---

### 4. Кулдауны и плейсхолдеры

```csharp
using Capy.Core.Features;
using Capy.Core.Services;

// Проверка и установка кулдауна для игрока
if (PlayerCooldown.HasCooldown(player.UserId, "ability_dash"))
{
    float remaining = PlayerCooldown.GetRemaining(player.UserId, "ability_dash");
    player.ShowHint($"Способность перезаряжается: {remaining:F1} сек.", 2f);
    return;
}

PlayerCooldown.SetCooldown(player.UserId, "ability_dash", TimeSpan.FromSeconds(15));

// Подстановка плейсхолдеров в текст
string rawText = "Привет, %player_name%! Онлайн: %server_players%/%server_max_players%.";
string formatted = PlaceholderReplacer.Replace(rawText, player);
// -> "Привет, Capybara! Онлайн: 18/25."
```
