using System.Collections.Generic;
using System.Linq;
using Capy.Engine.Hints;
using Exiled.API.Features;
using PlayerRoles;

namespace Capy.Engine.Hud.Panels;

/// <summary>
/// Панель отображения списка зрителей (наблюдателей), смотрящих за живым игроком.
/// </summary>
public sealed class SpectatorListPanel : HudPanel
{
    public SpectatorListPanel(Player player) : base(player) { }

    public override void Update()
    {
        if (!IsConnected || !Player.IsAlive || !Round.IsStarted)
        {
            SetText(string.Empty, HintZone.UpperRight);
            return;
        }

        var spectators = Player.CurrentSpectatingPlayers?
            .Where(p => p != null && p.IsConnected && p.Role.Type == RoleTypeId.Spectator)
            .Select(p => p.Nickname)
            .Take(5)
            .ToList() ?? new List<string>();

        if (spectators.Count == 0)
        {
            SetText(string.Empty, HintZone.UpperRight);
            return;
        }

        string namesStr = string.Join(", ", spectators);
        string text = $"<color=#ffa94e>👀 <b>Зрители ({spectators.Count}):</b></color> <color=#e0e0e0><size=16>{namesStr}</size></color>";

        SetText(text, HintZone.UpperRight, fontSize: 18, tag: "hud_spectator_list");
    }
}
