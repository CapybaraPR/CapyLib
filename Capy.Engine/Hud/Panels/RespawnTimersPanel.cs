using System;
using Capy.Engine.Hints;
using Exiled.API.Enums;
using Exiled.API.Features;
using PlayerRoles;
using Respawning.Waves;
using Respawning.Waves.Generic;

namespace Capy.Engine.Hud.Panels;

/// <summary>
/// Панель таймера следующей волны возрождения (МОГ / Хаос) для наблюдателей.
/// </summary>
public sealed class RespawnTimersPanel : HudPanel
{
    public RespawnTimersPanel(Player player) : base(player) { }

    public override void Update()
    {
        if (!IsConnected || Player.IsAlive || Player.Role.Type != RoleTypeId.Spectator || !Round.IsStarted)
        {
            SetText(string.Empty, HintZone.UpperRight);
            return;
        }

        double secMtf = 0;
        if (Respawn.TryGetWaveBase(SpawnableFaction.NtfWave, out var mtfWave) && mtfWave is TimeBasedWave timeMtf)
        {
            secMtf = Math.Max(0, timeMtf.Timer.TimeLeft);
        }

        double secChaos = 0;
        if (Respawn.TryGetWaveBase(SpawnableFaction.ChaosWave, out var chaosWave) && chaosWave is TimeBasedWave timeChaos)
        {
            secChaos = Math.Max(0, timeChaos.Timer.TimeLeft);
        }

        string mtfStr = TimeSpan.FromSeconds(Math.Ceiling(secMtf)).ToString(@"mm\:ss");
        string chaosStr = TimeSpan.FromSeconds(Math.Ceiling(secChaos)).ToString(@"mm\:ss");

        string text = $"<color=#6d9ff7>МОГ: {mtfStr}</color>  |  <color=#608f38>Хаос: {chaosStr}</color>";
        SetText(text, HintZone.UpperRight, fontSize: 17, tag: "hud_respawn_wave");
    }
}
