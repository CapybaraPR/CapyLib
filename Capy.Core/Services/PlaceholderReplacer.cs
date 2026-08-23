using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Capy.Core.Services;

public static class PlaceholderReplacer
{
    private static readonly ConcurrentDictionary<string, Func<Player?, string>> Resolvers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex PlaceholderRegex = new(@"%([a-zA-Z0-9_]+)%", RegexOptions.Compiled);

    public static void RegisterPlaceholder(string key, Func<Player?, string> resolver)
    {
        if (string.IsNullOrWhiteSpace(key) || resolver == null) return;
        Resolvers[key] = resolver;
    }

    public static void RegisterDefaults()
    {
        Resolvers.Clear();

        RegisterPlaceholder("player_name", p => p?.Nickname ?? "Unknown");
        RegisterPlaceholder("player_id", p => p?.Id.ToString() ?? "-1");
        RegisterPlaceholder("player_userid", p => p?.UserId ?? "None");
        RegisterPlaceholder("player_role", p => p?.Role.Type.ToString() ?? "None");
        RegisterPlaceholder("player_hp", p => p != null ? ((int)p.Health).ToString() : "0");
        RegisterPlaceholder("player_max_hp", p => p != null ? ((int)p.MaxHealth).ToString() : "0");
        RegisterPlaceholder("player_shield", p => p != null ? ((int)p.HumeShield).ToString() : "0");
        RegisterPlaceholder("player_kills", p => p?.SessionVariables.TryGetValue("Kills", out var k) == true ? k.ToString() : "0");
        RegisterPlaceholder("player_deaths", p => p?.SessionVariables.TryGetValue("Deaths", out var d) == true ? d.ToString() : "0");

        RegisterPlaceholder("server_name", _ => Server.Name);
        RegisterPlaceholder("server_ip", _ => Server.IpAddress);
        RegisterPlaceholder("server_port", _ => Server.Port.ToString());
        RegisterPlaceholder("server_players", _ => Player.List.Count().ToString());
        RegisterPlaceholder("server_max_players", _ => Server.PlayerCount.ToString());
        RegisterPlaceholder("server_tps", _ => Server.Tps.ToString("0.0"));

        RegisterPlaceholder("round_time", _ => Round.ElapsedTime.ToString(@"mm\:ss"));
        RegisterPlaceholder("round_status", _ => Round.IsStarted ? "Active" : "Waiting");
        RegisterPlaceholder("warhead_status", _ => Warhead.Status.ToString());
    }

    public static string Process(string template, Player? player = null)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        return PlaceholderRegex.Replace(template, match =>
        {
            string key = match.Groups[1].Value;
            if (Resolvers.TryGetValue(key, out var resolver))
            {
                try
                {
                    return resolver(player);
                }
                catch (Exception ex)
                {
                    Log.Error($"[PlaceholderReplacer] Ошибка в плейсхолдере '%{key}%': {ex.Message}");
                    return match.Value;
                }
            }
            return match.Value;
        });
    }
}
