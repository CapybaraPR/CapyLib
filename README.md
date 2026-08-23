# 🍊 CapyLib

> **Единый высокопроизводительный фреймворк и модульная библиотека для игровых серверов проекта Capybara (SCP: Secret Laboratory EXILED)**

```
===============================================================================
        (\_/)
       ( •_•)    ██████╗  █████╗ ██████╗ ██╗   ██╗██╗     ██╗██████╗ 
      / >🍊     ██╔════╝ ██╔══██╗██╔══██╗╚██╗ ██╔╝██║     ██║██╔══██╗
     /     \    ██║      ███████║██████╔╝ ╚████╔╝ ██║     ██║██████╔╝
    (_______)   ██║      ██╔══██║██╔═══╝   ╚██╔╝  ██║     ██║██╔══██╗
                ╚██████╗ ██║  ██║██║        ██║   ███████╗██║██████╔╝
                 ╚═════╝ ╚═╝  ╚═╝╚═╝        ╚═╝   ╚══════╝╚═╝╚═════╝ 

          [ Capybara Framework & Library v1.4.0 for SCP:SL EXILED ]
===============================================================================
```

---

## 📖 О проекте

**CapyLib** — это комплексный фундамент для разработки серверов и плагинов SCP:SL на базе EXILED. Библиотека объединила и развила лучшие наработки **AspectLib**, **SpecterLib**, **Redox-Engine** и **Hazbin.Core**, предоставляя разработчикам богатый инструментарий для управления сетевым кодом, байткод-патчами, сущностями, базами данных, серверными биндами, эффектами, русской локализацией и пиксельно-точным экранным интерфейсом (HUD).

Все компоненты компилируются в единую оптимизированную сборку **`CapyLib.dll`** под `.NET Framework 4.8`.

---

## 📚 Документация основных систем

| Система | Описание | Ссылка на руководство |
| :--- | :--- | :--- |
| 🧠 **`Capy.Core`** | Архитектура ядра, модульный загрузчик DAG, IoC-контейнер, DRM, базы данных (LiteDB / MongoDB), EventBus, Enum-локализация и сетевые расширения. | [**Руководство по Capy.Core ➔**](./Capy.Core/README.md) |
| 🌐 **`Capy.API`** | Сетевой слой, защищенный Discord-мост (SSH RSA подписи), REST API, двусторонние вебхуки и телеметрия. | [**Руководство по Capy.API ➔**](./Capy.API/README.md) |
| ⚙️ **`Capy.Engine`** | Игровой движок, пиксельный HUD (`AspectLib HintService`), ServerSpecific бинды (`AssKeybinds`), Harmony-патчи, Mirror FakeSync, кастомные предметы и роли. | [**Руководство по Capy.Engine ➔**](./Capy.Engine/README.md) |

---

## 🏛️ Архитектура и функционал

### 1. ⚙️ `Capy.Core` — Фундамент, утилиты и локализация

* **Модульная система (`ModuleLoader`, `ModuleManager`)**:
  * Динамический поиск и рефлексивная загрузка модулей с разрешением графа зависимостей через `[DependsOn]`.
  * Полное рантайм-управление: включение (`Enable`), выключение (`Disable`), переключение (`Toggle`) и горячий перезапуск (`Restart`) модулей без рестарта сервера.
* **Мульти-файловый YAML (`ConfigLoader`)**:
  * Изолированные конфиги для каждого модуля по пути `Plugins/CapyLib/Configs/<ModuleName>.yml`.
* **DRM и Лицензирование (`LicenseManager`)**:
  * HTTP-валидация ключей сервера с поддержкой автоматического режима разработчика (`DEV_LICENSE` / `validate_license: false`).
* **Базы данных (`IDatabaseProvider`)**:
  * **LiteDB** — встроенная NoSQL база данных (`Plugins/CapyLib/Database/CapyData.db`).
  * **MongoDB** — высокопроизводительный драйвер для сети распределённых серверов.
* **Шина событий (`EventBus`)**:
  * Потокобезопасная Pub/Sub шина для слабой связанности компонентов.
* **Сетевые расширения Mirror (`NetworkExtensions`)**:
  * `ChangeAppearance(player, roleType)` — маскировка и подмена модели игрока на сетевом уровне пакетов (без эффектов и артефактов).
  * `SendFakeRPC` / `SendFakeTargetRpc` — отправка изолированных RPC-пакетов.
  * `PlayCassieAnnouncement` / `SendFakeCassieMessage` — персонализированные аудио-оповещения C.A.S.S.I.E.
* **🇷🇺 Русская локализация Enum'ов (`EnumExtensions`, `EffectTypeExtensions`)**:
  * Универсальный метод `.Translate(bool useColors = false)` и алиасы `.GetTranslation()`, `.ToRussian()` для **всех** перечислений:
    * `EffectType` (все эффекты SCP:SL с нативной цветовой подсветкой)
    * `ItemType` (все игровые предметы, карточки, оружие)
    * `RoleTypeId` (все роли фонда, хаоса, SCP и людей)
    * `RoomType` и `ZoneType` (все комнаты и зоны комплекса)
    * `DamageType`, `Side`, `AmmoType`

---

### 2. 🖥️ `Capy.Engine.Hints` & `Capy.Engine.Hud` — Пиксельный движок интерфейса

* **Пиксельный рендеринг TextMeshPro (`PlayerDisplay`)**:
  * Полный порт высокоточного движка из **AspectLib / SpecterLib**.
  * Единый холст `1080p` с расчетом координат `X` (`-960..+960`) и `Y` (`0..1080`).
  * Верхние и нижние якоря `<voffset=9999>P</voffset>` и `<voffset=-9999>P</voffset>`, исключающие сжатие контейнера и разрыв слов.
  * Автоматическое закрытие незакрытых HTML-тегов (`HintStringBuilder.RepairUnclosedTags`).
* **Модульная система панелей (`HudManager` & `HudPanel`)**:
  * `RoundTimePanel` — время раунда по центру сверху (`Y: 10`).
  * `SpectatorBottomPanel` — статус наблюдаемого игрока, здоровье, предмет в руках и онлайн (`Y: 950`).
  * `SpectatorListPanel` — список наблюдателей за живым игроком (`Y: 350, Right`).
  * `RespawnMtfTimerPanel` & `RespawnChaosTimerPanel` — таймеры прибытия подкреплений МОГ / Хаос.
  * `ItemHudPanel` — адаптивная плашка способностей кастомных предметов над HP (`HudLayout.GetDynamicStatsPosition`).
* **Удобные методы расширения (`ShowHintExtensions`)**:
  * `player.ShowHint(text, position, duration, ...)`
  * `player.ShowZoneHint(HintZone.TopCenter / Notification / BottomCenter, text, duration, ...)`

---

### 3. ⌨️ `Capy.Engine.ServerSpecific` — Серверные бинды (`AssKeybinds`)

* **Интеграция с ServerSpecificSettings**:
  * Регистрация серверных клавиш в меню настроек игры клиента: **`ЛКМ`**, **`ПКМ`**, **`E`**, **`F`**, **`T`**, **`B`**, **`G`**, **`ALT`**, **`R`**, стрелочки **`⬆⬇⬅➡`**, **`Backspace`**.
  * Глобальные события `AssKeybinds.OnKeybindPressed` и `AssKeybinds.OnKeybindReleased`.
  * Проверка зажатия в реальном времени через `AssKeybinds.IsPressed(player, keybind)`.

---

### 4. 🛡️ Байткод-патчи, FakeSync и кастомный контент

* **Harmony-патчи**:
  * Перехват клиентской консоли (`~`), удаление системного мусора движка.
  * Защита от несанкционированной выдачи ролей (`SecurityPatches`).
* **Сетевая подмена данных (`FakeSync`)**:
  * `FakeSyncVar` / `FakeSyncList` / `FakeRpc` — персональная подмена сетевых переменных, ролей, предметов и игрушек.
* **Кастомный контент**:
  * `CustomItem` / `CustomItemsManager` — кастомные предметы с подсветкой, спавном и способностями.
  * `CustomRole` / `CustomRolesManager` — кастомные роли с экипировкой и моделями.
* **3D-аудио**:
  * `AudioRegistry` — воспроизведение `.ogg` звуков с пространственной привязкой к игрокам и координатам.

---

## 📦 Сборка и установка

```bash
dotnet build CapyLib.csproj -c Release
```
Готовый бинарник: `bin/Release/net48/CapyLib.dll`.  
Копируется в директорию плагинов сервера EXILED:
`~/.config/EXILED/Plugins/7777/CapyLib.dll`