using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Models;
using Hint = Capy.Engine.Hints.Models.Hint;
using Capy.Engine.Hints.Utilities;
using Exiled.API.Features;
using Capy.Engine.Hud.Config;

namespace Capy.Engine.Hud.Panels;

public class AliveBrandPanel : HudPanel
{
    private string _lastText = string.Empty;

    public AliveBrandPanel(Player player) : base(player) { }

    protected override void CreateHint(PlayerDisplay display)
    {
        // Very bottom of screen - Top align with high Y pushes it down
        Hint = new Hint
        {
            Text = string.Empty,
            FontSize = 25,
            Alignment = HintAlignment.Center,
            XCoordinate = 0,
            YCoordinate = 1050,
            YCoordinateAlign = HintVerticalAlign.Top,
            SyncSpeed = HintSyncSpeed.Fast
        };
        display.AddHint(Hint);
    }

    public override void Update()
    {
        if (!IsPlayerConnected || !Player.IsAlive)
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

        string text = config.ServerBrandHintText;
        if (text != _lastText)
        {
            SetText(text);
            _lastText = text;
        }
    }
}
