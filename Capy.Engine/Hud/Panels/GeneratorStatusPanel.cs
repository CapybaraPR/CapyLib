using System;
using System.Linq;
using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Models;
using Capy.Engine.Hints.Utilities;
using Exiled.API.Features;
using MapGeneration.Distributors;
using Hint = Capy.Engine.Hints.Models.Hint;

namespace Capy.Engine.Hud.Panels;

public class GeneratorStatusPanel : HudPanel
{
    private string _lastText = string.Empty;

    public GeneratorStatusPanel(Player player) : base(player) { }

    protected override void CreateHint(PlayerDisplay display)
    {
        Hint = new Hint
        {
            Text = string.Empty,
            FontSize = 15,
            Alignment = HintAlignment.Center,
            XCoordinate = 0,
            YCoordinate = 82,
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

        var generators = Generator.List.ToList();
        if (generators.Count == 0) return;

        int engaged = generators.Count(g => g.IsEngaged);
        int total = generators.Count;

        // Показываем панель генераторов, если хоть 1 генератор задействован или активируется
        bool isAnyActivating = generators.Any(g => g.IsActivating);
        if (engaged == 0 && !isAnyActivating)
        {
            if (!string.IsNullOrEmpty(_lastText))
            {
                SetText(string.Empty);
                _lastText = string.Empty;
            }
            return;
        }

        if (!EnsureHint()) return;

        string text = $"<b><color=#00FFFF>⚡ Генераторы SCP-079: {engaged}/{total} задействовано</color></b>";

        if (text != _lastText)
        {
            SetText(text);
            _lastText = text;
        }
    }
}
