# 🌐 Capy.API — Сетевой и интеграционный слой

`Capy.API` объединяет все внешние сетевые сервисы, HTTP API, систему защищенного управления через Discord (DiscordBridge), отправку вебхуков, асинхронные HTTP-клиенты и сбор игровой аналитики для серверов **CapyLib**.

---

## 📁 Структура `Capy.API`

```
Capy.API/
├── Bridges/        # DTO-модели (ServerStatusDto, PlayerInfoDto) и интерфейсы мостов
├── DiscordBridge/  # Полнофункциональный Discord-мост с SSH RSA аутентификацией
├── Http/           # Асинхронный HTTP/REST клиент (CapyApiClient)
├── Stats/          # Модуль сбора, агрегации и выгрузки статистики игроков
└── Webhooks/       # Диспетчер отправки Embed-вебхуков в Discord и сторонние сервисы
```

---

## 🛠️ Примеры использования

### 1. Защищенный Discord-мост (`DiscordBridge`)

Обеспечивает удаленное управление сервером через бота с проверкой подписи по **SSH RSA ключам** и защитой от Replay-атак:

* **Выполнение RemoteAdmin команд из Discord**:
  Бот отправляет подписанный запрос на `/v1/command`, сервер проверяет цифровую подпись и выполняет команду в основном игровом потоке через `MainThreadDispatcher`.
* **5 категорий логирования**:
  Все баны, муты, смерти, старты/концы раундов, взрывы боеголовки и репорты игроков автоматически буферизируются и выгружаются ботом в соответствующие Discord-каналы.
* **Привязка Steam/Discord и авто-роли**:
  Игрок вводит `/steamsl` в Discord, получает 6-значный код и вводит его в консоль игры (`.linkdiscord <code>`). Сервер мгновенно выдает соответствующую группу прав в игре (`permissions.yml`).

---

### 2. Отправка вебхуков (`WebhookDispatcher`)

Отправка форматированных сообщений в каналы Discord без блокировки основного потока:

```csharp
using Capy.API.Webhooks;

// Отправка Embed-вебхука о важном событии
WebhookDispatcher.SendEmbed(
    webhookUrl: "https://discord.com/api/webhooks/...",
    title: "🚨 Активация Альфа-Боеголовки",
    description: $"Игрок **{player.Nickname}** запустил обратный отсчет детонации!",
    colorHex: "#FF0000",
    fields: new List<WebhookField>
    {
        new("Инициатор", $"{player.Nickname} ({player.UserId})", inline: true),
        new("Время до взрыва", "90 сек.", inline: true)
    }
);
```

---

### 3. Асинхронные HTTP-запросы (`CapyApiClient`)

Удобный асинхронный клиент с обработкой таймаутов и сериализацией JSON:

```csharp
using Capy.API.Http;

// Отправка данных на внешний сайт проекта
public async Task SyncPlayerBanAsync(string steamId, string reason, long duration)
{
    var payload = new 
    { 
        steam_id = steamId, 
        reason = reason, 
        duration = duration,
        server = "NR-1" 
    };

    var response = await CapyApiClient.PostAsync("https://api.myserver.ru/v1/bans", payload);
    if (response.IsSuccessStatusCode)
    {
        Log.Info("Информация о бане успешно отправлена на центральный сайт.");
    }
}
```

---

### 4. Сбор и обработка статистики (`StatsModule`)

Автоматический трекинг прогресса игроков за раунд:

```csharp
using Capy.API.Stats;

// Доступ к сессионной статистике раунда
var stats = StatsModule.CurrentRoundStats;
Log.Info($"Всего убийств за раунд: {stats.TotalKills}");
Log.Info($"Лучший игрок по урону: {stats.TopDamager?.Nickname} ({stats.TopDamageValue} HP)");
```
