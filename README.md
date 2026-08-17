# 🍊 CapyLib

> **Единый фреймворк и модульная библиотека для игровых серверов проекта Capybara (SCP: Secret Laboratory EXILED)**

```
===============================================================================
        (\_/)
       ( •_•)    ██████╗  █████╗ ██████╗ ██╗   ██╗██╗     ██╗██████╗
      / >🍊     ██╔════╝ ██╔══██╗██╔══██╗╚██╗ ██╔╝██║     ██║██╔══██╗
     /     \    ██║      ███████║██████╔╝ ╚████╔╝ ██║     ██║██████╔╝
    (_______)   ██║      ██╔══██║██╔═══╝   ╚██╔╝  ██║     ██║██╔══██╗
                ╚██████╗ ██║  ██║██║        ██║   ███████╗██║██████╔╝
                 ╚═════╝ ╚═╝  ╚═╝╚═╝        ╚═╝   ╚══════╝╚═╝╚═════╝

          [ Capybara Framework & Library v1.1.0 for SCP:SL EXILED ]
===============================================================================
```

---

## 📖 О проекте

**CapyLib** — это высокопроизводительное модульное ядро, объединившее лучшие архитектурные решения **AspectLib** и **Redox-Engine**. Библиотека компилируется в единую сборку `CapyLib.dll` и предназначена для подключения к специализированным плагинам игровых серверов (MediumRP, NoRules и др.).

---

## 🏛️ Архитектура

Библиотека разделена на 3 основных слоя:

### 1. `Capy.Core` — Фундамент
* **Модульная система**: Рефлексивная загрузка (`ModuleLoader`) с топологической сортировкой зависимостей `[DependsOn]`.
* **Рантайм-управление (`ModuleManager`)**: Включение, выключение, перезагрузка модулей на лету без рестарта сервера.
* **Мульти-файловый YAML (`ConfigLoader`)**: Каждый модуль хранит свой конфиг в отдельном файле `Plugins/CapyLib/Configs/<Module>.yml`.
* **DRM-лицензирование (`LicenseManager`)**: Защита сервера с проверкой лицензионного ключа через HTTP API.
* **Шина сообщений (`EventBus`)**: Потокобезопасный Pub/Sub для слабой связанности компонентов.
* **Авто-регистрация событий (`EventRegistrar`)**: Автоматическая подписка методов на события EXILED по типам аргументов.
* **База данных**: Унифицированный интерфейс `IDatabaseProvider` с поддержкой **LiteDB** (локально) и **MongoDB** (для сети серверов).
* **Плейсхолдеры (`PlaceholderReplacer`)**: Динамическая замена `%player_name%`, `%server_tps%`, `%round_time%` и др.

### 2. `Capy.API` — Внешние интеграции
* **HTTP клиент (`CapyApiClient`)**: Высокоскоростной асинхронный клиент для взаимодействия с ботом и веб-панелью.
* **Телеметрия (`StatsModule`)**: Сбор игровой статистики (Kills, Deaths, Playtime) и логов входа/выхода администраторов и игроков.
* **Мосты (`IBotBridge`, `IWebBridge`)**: Контракты для Discord/Telegram ботов и веб-сайта.
* **Вебхуки (`WebhookDispatcher`)**: Отправка игровых событий во внешние сервисы.

### 3. `Capy.Engine` — Игровой инструментарий
* **Кастомные предметы (`CustomItem`, `CustomItemsManager`, `SpawnManager`)**:
  * Базовые абстракции для создания предметов серверов.
  * Интерфейсы возможностей: `ISpawnableItem`, `IGlowingItem`, `IUsableItem`.
  * Привязка к комнатам карты (`RoomSpawnPoint`).
* **Кастомные роли (`CustomRole`, `CustomRolesManager`)**:
  * Базовые классы для ролей (настройка HP, Hume Shield, инвентаря, патронов).
* **UI и подсказки (`PlayerDisplay`, `ShowHintExtensions`)**:
  * Батчевый рендерер 20 FPS через Mirror-пакеты.
  * Удобные хинты `player.ShowCapyHint()` с автоматическим стэкингом.
* **Пространственное аудио (`AudioRegistry`, `AudioToggle`)**:
  * Загрузка `.ogg` файлов из `Plugins/CapyLib/Audio/`.
  * Привязка 3D-звука к игрокам, объектам, комнатам или глобально.
* **Эффекты (`ItemGlowSystem`)**:
  * Визуальное свечение предметов и подсвечивание на полу.
* **Серверные профили**:
  * `CapyMediumRPPlugin` — базовый класс плагина для серверов **MediumRP**.
  * `CapyNoRulesPlugin` — базовый класс плагина для серверов **NoRules**.

---

## 🛠️ Админ-команды

Управление модулями доступно через консоль сервера или в игре администраторам (по SteamID):

| Команда | Описание |
| :--- | :--- |
| `.capy list` (или `.al list`) | Список всех модулей и их статус |
| `.capy toggle <имя>` | Включить / выключить модуль |
| `.capy enable <имя>` | Включить модуль |
| `.capy disable <имя>` | Выключить модуль |
| `.capy restart <имя>` | Перезапустить модуль с перезагрузкой конфига |
| `.capy reload` | Перечитать все YAML-конфигурации с диска |

---

## 🚀 Пример создания серверного плагина

```csharp
using Capy.Engine.NoRules;
using Capy.Engine.CustomItems.Base;
using Capy.Core.Loader;

namespace MyNoRulesServer;

public class MyPlugin : CapyNoRulesPlugin
{
    public override string Name => "Capybara NoRules Server";
    public override string Author => "CapybaraPR";

    public override void ConfigureNoRulesModules()
    {
        // Включение нужных модулей
        ModuleManager.Enable("StatsTracker", out _);
    }

    public override void RegisterNoRulesContent()
    {
        // Регистрация кастомных предметов сервера
        CustomItemsManager.Register(new MyCustomWeapon());
    }
}
```

---

## 📦 Сборка проекта

```bash
dotnet build CapyLib.csproj -c Release
```

Выходной файл: `bin/Release/net48/CapyLib.dll`.