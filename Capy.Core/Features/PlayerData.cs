namespace Capy.Core.Features;

public class PlayerData
{
    private static readonly Dictionary<string, PlayerData> Store = new(StringComparer.OrdinalIgnoreCase);

    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, List<object>> Data { get; set; } = new();

    public static void Add(string id, string key, List<object> data)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(key)) return;

        if (!Store.TryGetValue(id, out var entry))
        {
            entry = new PlayerData { UserId = id };
            Store[id] = entry;
        }

        entry.Data[key] = data;
    }

    public static bool Contains(string id) => !string.IsNullOrEmpty(id) && Store.ContainsKey(id);

    public static PlayerData? Get(string id) =>
        string.IsNullOrEmpty(id) ? null : Store.TryGetValue(id, out var entry) ? entry : null;

    public static bool TryGet(string id, out PlayerData? data)
    {
        data = Get(id);
        return data != null;
    }

    public static bool Remove(string id) => !string.IsNullOrEmpty(id) && Store.Remove(id);
}
