using System;
using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Models;
using Hint = Capy.Engine.Hints.Models.Hint;
using Capy.Engine.Hints.Utilities;
using Capy.Engine.Hud.Config;
using Exiled.API.Enums;
using Exiled.API.Features;
using Respawning.Waves;
using Respawning.Waves.Generic;

namespace Capy.Engine.Hud.Panels;

public static class CustomRespawnTimerProvider
{
    public delegate bool GetRespawnInfoDelegate(out string text, out double secondsLeft, out string colorHex, out bool isKnown);
    public static GetRespawnInfoDelegate? Provider { get; set; }
}

/// <summary>
/// Единая панель таймера подкрепления для наблюдателей:
/// - Пока неизвестно кто едет: нейтральный таймер «До подкрепления: mm:ss»
/// - Когда контроллер/игра определила отряд:
///   - «Разведгруппа МОГ прибывает через: mm:ss»
///   - «Аварийный отряд МОГ прибывает через: mm:ss»
///   - «Отряд CO2 прибывает через: mm:ss»
///   - «Хаос прибывает через: mm:ss»
/// </summary>
public class RespawnTimerPanel : HudPanel
{
    private string _lastText = string.Empty;

    public RespawnTimerPanel(Player player) : base(player) { }

    protected override void CreateHint(PlayerDisplay display)
    {
        Hint = new Hint
        {
            Text = string.Empty,
            FontSize = 18,
            Alignment = HintAlignment.Center,
            XCoordinate = 0,
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

        var config = CapyPlugin.Instance?.Config?.Hud ?? new HudConfig();
        if (config == null || !config.Enabled || !config.ShowRespawnTimers)
        {
            if (!string.IsNullOrEmpty(_lastText))
            {
                SetText(string.Empty);
                _lastText = string.Empty;
            }
            return;
        }

        // Если кастомный контроллер спавна (например, SpawnManager в NoRules) предоставляет точную информацию
        if (CustomRespawnTimerProvider.Provider != null && CustomRespawnTimerProvider.Provider(out var customSquad, out var secLeft, out var squadColor, out var isKnown))
        {
            string customText;
            if (secLeft < 0)
            {
                customText = $"<size=18><color={squadColor}><b>{customSquad}</b></color></size>";
            }
            else
            {
                string tCustom = TimeSpan.FromSeconds(Math.Ceiling(secLeft)).ToString(@"mm\:ss");
                customText = isKnown
                    ? $"<size=18><color={squadColor}>{customSquad} прибывает через: <b>{tCustom}</b></color></size>"
                    : $"<size=18><color=#cbd5e1>До подкрепления: <b>{tCustom}</b></color></size>";
            }

            if (customText != _lastText)
            {
                SetText(customText);
                _lastText = customText;
            }
            return;
        }

        double secMtf = 0;
        if (Respawn.TryGetWaveBase(SpawnableFaction.NtfWave, out var waveMtf) && waveMtf is TimeBasedWave timeBasedMtf)
        {
            secMtf = Math.Max(0, timeBasedMtf.Timer.TimeLeft);
        }

        double secChaos = 0;
        if (Respawn.TryGetWaveBase(SpawnableFaction.ChaosWave, out var waveChaos) && waveChaos is TimeBasedWave timeBasedChaos)
        {
            secChaos = Math.Max(0, timeBasedChaos.Timer.TimeLeft);
        }

        var nextFaction = Respawn.NextKnownSpawnableFaction;

        string text;
        if (nextFaction == SpawnableFaction.NtfWave)
        {
            string tP = TimeSpan.FromSeconds(Math.Ceiling(secMtf)).ToString(@"mm\:ss");
            text = $"<size=18><color=#6D9FF7>МОГ прибывает через: <b>{tP}</b></color></size>";
        }
        else if (nextFaction == SpawnableFaction.ChaosWave)
        {
            string tP = TimeSpan.FromSeconds(Math.Ceiling(secChaos)).ToString(@"mm\:ss");
            text = $"<size=18><color=#608F38>Хаос прибывает через: <b>{tP}</b></color></size>";
        }
        else
        {
            double nearest = Math.Min(secMtf > 0 ? secMtf : double.MaxValue, secChaos > 0 ? secChaos : double.MaxValue);
            if (nearest == double.MaxValue || nearest <= 0)
                nearest = Math.Max(secMtf, secChaos);

            if (nearest <= 20)
            {
                if (secMtf < secChaos && secMtf > 0)
                {
                    string tP = TimeSpan.FromSeconds(Math.Ceiling(secMtf)).ToString(@"mm\:ss");
                    text = $"<size=18><color=#6D9FF7>МОГ прибывает через: <b>{tP}</b></color></size>";
                }
                else if (secChaos > 0)
                {
                    string tP = TimeSpan.FromSeconds(Math.Ceiling(secChaos)).ToString(@"mm\:ss");
                    text = $"<size=18><color=#608F38>Хаос прибывает через: <b>{tP}</b></color></size>";
                }
                else
                {
                    string tP = TimeSpan.FromSeconds(Math.Ceiling(nearest)).ToString(@"mm\:ss");
                    text = $"<size=18><color=#cbd5e1>До подкрепления: <b>{tP}</b></color></size>";
                }
            }
            else
            {
                string tP = TimeSpan.FromSeconds(Math.Ceiling(nearest)).ToString(@"mm\:ss");
                text = $"<size=18><color=#cbd5e1>До подкрепления: <b>{tP}</b></color></size>";
            }
        }

        if (text != _lastText)
        {
            SetText(text);
            _lastText = text;
        }
    }
}
