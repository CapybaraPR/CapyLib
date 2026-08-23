using System;
using System.Linq;
using Capy.Engine.CustomItems.Manager;
using Capy.Engine.CustomRoles.Manager;
using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Models;
using Capy.Engine.Hints.Utilities;
using Capy.Engine.Hud.Config;
using Exiled.API.Features;
using PlayerRoles;
using Hint = Capy.Engine.Hints.Models.Hint;

namespace Capy.Engine.Hud.Panels;

public class SpectatorBottomPanel : HudPanel
{
    private string _lastText = string.Empty;

    public SpectatorBottomPanel(Player player) : base(player) { }

    protected override void CreateHint(PlayerDisplay display)
    {
        Hint = new Hint
        {
            Text = string.Empty,
            FontSize = 20,
            Alignment = HintAlignment.Center,
            XCoordinate = 0,
            YCoordinate = 950,
            YCoordinateAlign = HintVerticalAlign.Top,
            SyncSpeed = HintSyncSpeed.Fast
        };
        display.AddHint(Hint);
    }

    public override void Update()
    {
        if (!IsPlayerConnected || Player.IsAlive
            || Player.Role.Type is RoleTypeId.Overwatch or RoleTypeId.Destroyed
            || !Round.IsStarted)
        {
            if (!string.IsNullOrEmpty(_lastText))
            {
                SetText(string.Empty);
                _lastText = string.Empty;
            }
            return;
        }

        if (!EnsureHint()) return;

        var config = CapyPlugin.Instance?.Config?.Hud ?? new HudConfig();
        if (config == null || !config.Enabled) return;

        string spectating = "—";
        string roleInfo = string.Empty;
        string hpInfo = string.Empty;
        string itemInfo = string.Empty;

        var target = Player.List
            .Where(p => p is not null && p.IsAlive)
            .FirstOrDefault(p => p.CurrentSpectatingPlayers != null && p.CurrentSpectatingPlayers.Contains(Player));

        if (target != null)
        {
            spectating = target.Nickname;

            var customRole = CustomRolesManager.GetRole(target);
            if (customRole != null)
            {
                roleInfo = $" <color=#ffa94e><b>[{customRole.Name}]</b></color>";
            }
            else
            {
                string hex = UnityEngine.ColorUtility.ToHtmlStringRGBA(target.Role.Color);
                roleInfo = $" <color=#{hex}><b>[{target.Role.Name}]</b></color>";
            }

            int hp = (int)Math.Max(0, target.Health);
            int maxHp = (int)target.MaxHealth;
            int ahp = (int)target.ArtificialHealth;
            string ahpText = ahp > 0 ? $" <color=#70c0ff>(+{ahp} AHP)</color>" : "";
            hpInfo = $" | <color=#ff4444>❤️ <b>{hp}</b>/{maxHp} HP</color>{ahpText}";

            if (target.CurrentItem != null)
            {
                itemInfo = $" | ✋ <color=#a3e635>{target.CurrentItem.Type}</color>";
            }
        }

        string fullSpectatingStr = $"{spectating}{roleInfo}{hpInfo}{itemInfo}";

        string text = config.SpectatorBottomPanel
            .Replace("{spectating}", fullSpectatingStr)
            .Replace("{playersCurrent}", Player.List.Count(p => p is not null && !p.IsHost && p.IsAlive && p.Role.Type != RoleTypeId.Tutorial).ToString())
            .Replace("{spectators}", Player.List.Count(p => p is not null && !p.IsAlive && !p.IsHost && p.Role.Type == RoleTypeId.Spectator).ToString())
            .Replace("{playersMax}", Server.MaxPlayerCount.ToString())
            .Replace("{serverBrand}", config.ServerBrandHintText);

        if (text != _lastText)
        {
            SetText(text);
            _lastText = text;
        }
    }
}
