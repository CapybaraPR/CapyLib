using System.Collections.Concurrent;
using System.Text;

namespace Capy.Engine.Hints.Service;

public class PlayerDisplay
{
    private static readonly ConcurrentDictionary<int, PlayerDisplay> Displays = new();

    public Player Player { get; }
    private readonly List<AbstractHint> _hints = new();
    private readonly object _lock = new();

    public PlayerDisplay(Player player)
    {
        Player = player;
    }

    public static PlayerDisplay Get(Player player)
    {
        return Displays.GetOrAdd(player.Id, _ => new PlayerDisplay(player));
    }

    public static void Remove(Player player)
    {
        Remove(player.Id);
    }

    public static void Remove(int playerId)
    {
        if (Displays.TryRemove(playerId, out var display))
            display.ClearHint();
    }

    public void AddHint(AbstractHint hint)
    {
        lock (_lock)
        {
            _hints.Add(hint);
            Render();
        }
    }

    public void RemoveHint(AbstractHint hint)
    {
        lock (_lock)
        {
            if (_hints.Remove(hint))
                Render();
        }
    }

    public void ClearHint()
    {
        lock (_lock)
        {
            _hints.Clear();
        }
    }

    private void Render()
    {
        if (Player == null || !Player.IsConnected) return;

        var sb = new StringBuilder();
        var sorted = _hints.OrderBy(h => (int)h.Layer).ThenBy(h => h.Priority).ToList();

        foreach (var hint in sorted)
        {
            if (!string.IsNullOrEmpty(hint.Text))
            {
                sb.AppendLine($"<size={hint.FontSize}>{hint.Text}</size>");
            }
        }

        string finalContent = sb.ToString();
        if (!string.IsNullOrEmpty(finalContent))
        {
            Player.ShowHint(finalContent, 2f);
        }
        else
        {
            Player.ShowHint(string.Empty, 0.1f);
        }
    }
}
