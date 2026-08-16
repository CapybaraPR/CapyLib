using Capy.API.Bridges.Models;
using Capy.API.Http;
using Capy.Core.API;

namespace Capy.API.Webhooks;

/// <summary>
/// Конфигурация исходящих вебхуков.
/// </summary>
public class WebhookConfig : IModuleConfig
{
    public bool DebugMode { get; set; } = false;
    public List<string> WebhookUrls { get; set; } = new();
    public bool SendOnRoundStart { get; set; } = true;
    public bool SendOnRoundEnd { get; set; } = true;
    public bool SendOnWarheadDetonate { get; set; } = true;
}

/// <summary>
/// Диспетчер отправки игровых событий во внешние вебхуки (Discord/HTTP).
/// </summary>
public static class WebhookDispatcher
{
    public static WebhookConfig Config { get; set; } = new();

    public static void Dispatch(GameEventDto ev)
    {
        if (Config == null || Config.WebhookUrls == null || Config.WebhookUrls.Count == 0) return;

        foreach (var url in Config.WebhookUrls)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                _ = CapyApiClient.PostFireAndForgetAsync(url, ev);
            }
        }
    }
}
