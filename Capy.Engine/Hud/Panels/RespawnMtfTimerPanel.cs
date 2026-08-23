using System;
using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Models;
using Hint = Capy.Engine.Hints.Models.Hint;
using Capy.Engine.Hints.Utilities;
using Exiled.API.Features;
using Exiled.API.Enums;
using Respawning.Waves;
using Respawning.Waves.Generic;
using UnityEngine;

namespace Capy.Engine.Hud.Panels;

public class RespawnMtfTimerPanel : HudPanel
{
    private string _lastText = string.Empty;

    public RespawnMtfTimerPanel(Player player) : base(player) { }

    protected override void CreateHint(PlayerDisplay display)
    {
        Hint = new Hint
        {
            Text = string.Empty,
            FontSize = 18,
            Alignment = HintAlignment.Center,
            XCoordinate = -430,
            YCoordinate = 82,
            YCoordinateAlign = HintVerticalAlign.Middle,
            SyncSpeed = HintSyncSpeed.Fast
        };
        display.AddHint(Hint);
    }

    public override void Update()
    {
        if (!IsPlayerConnected || Player.IsAlive || !Round.IsStarted)
        {
            if (!string.IsNullOrEmpty(_lastText))
            {
                SetText(string.Empty);
                _lastText = string.Empty;
            }
            return;
        }

        if (!EnsureHint()) return;

        double secP = 0;
        if (Respawn.TryGetWaveBase(SpawnableFaction.NtfWave, out var wave) && wave is TimeBasedWave timeBasedWave)
        {
            secP = Math.Max(0, timeBasedWave.Timer.TimeLeft);
        }

        string tP = TimeSpan.FromSeconds(Math.Ceiling(secP)).ToString(@"mm\:ss");

        string text = $"<size=18><color=#6D9FF7>До прибытия: {tP}</color></size>";

        if (text != _lastText)
        {
            SetText(text);
            _lastText = text;
        }
    }
}
