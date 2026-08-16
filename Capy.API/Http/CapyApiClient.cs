using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Capy.API.Http;

/// <summary>
/// Высокопроизводительный асинхронный HTTP клиент для взаимодействия с внешними API (бот, сайт, телеметрия).
/// </summary>
public static class CapyApiClient
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseString, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[CapyApiClient] Ошибка POST запроса к {url}: {ex.Message}");
        }
        return default;
    }

    public static async Task<bool> PostFireAndForgetAsync(string url, object payload)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Client.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Log.Debug($"[CapyApiClient] Ошибка фоновой отправки на {url}: {ex.Message}");
            return false;
        }
    }
}
