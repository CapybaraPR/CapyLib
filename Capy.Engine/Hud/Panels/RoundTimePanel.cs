using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Models;
using Hint = Capy.Engine.Hints.Models.Hint;
using Capy.Engine.Hints.Utilities;
using Exiled.API.Features;
using PlayerRoles;
using Capy.Engine.Hud.Config;

namespace Capy.Engine.Hud.Panels;

public class RoundTimePanel : HudPanel
{
    private string _lastText = string.Empty;

    public RoundTimePanel(Player player) : base(player) { }

    protected override void CreateHint(PlayerDisplay display)
    {
        // Exact FlamingoHUD: Vector2(0, 10), HintVerticalAlign.Top, HintAlignment.Center, FontSize 16
        Hint = new Hint
        {
            Text = string.Empty,
            FontSize = 16,
            Alignment = HintAlignment.Center,
            XCoordinate = 0,
            YCoordinate = 10,
            YCoordinateAlign = HintVerticalAlign.Top,
            SyncSpeed = HintSyncSpeed.Fast
        };
        display.AddHint(Hint);
    }

    public override void Update()
    {
        if (!IsPlayerConnected || !Round.IsStarted)
        {
            if (!string.IsNullOrEmpty(_lastText))
            {
                SetText(string.Empty);
                _lastText = string.Empty;
            }
            return;
        }

        if (!EnsureHint()) return;

        var config = CapyPlugin.Instance?.Config?.Hud ?? new Capy.Engine.Hud.Config.HudConfig();
        if (config == null || !config.Enabled) return;

        var elapsedTime = Round.ElapsedTime;
        string duration = elapsedTime.TotalHours >= 1
            ? elapsedTime.ToString(@"hh\:mm\:ss")
            : elapsedTime.ToString(@"mm\:ss");

        string color;
        if (Player.Role.Team == Team.SCPs)                  color = "#FF0000";
        else if (Player.Role.Team == Team.FoundationForces)  color = "#6D9FF7";
        else if (Player.Role.Team == Team.ChaosInsurgency)   color = "#608F38";
        else if (Player.Role.Type == RoleTypeId.ClassD)        color = "#FFA500";
        else if (Player.Role.Type == RoleTypeId.Scientist)     color = "#FFCE1B";
        else if (Player.Role.Type == RoleTypeId.FacilityGuard) color = "#898989";
        else color = "#ffa94e";

        string text = config.RoundTimeHintText.Replace("{color}", color).Replace("{roundTime}", duration);

        if (text != _lastText)
        {
            SetText(text);
            _lastText = text;
        }
    }
}
