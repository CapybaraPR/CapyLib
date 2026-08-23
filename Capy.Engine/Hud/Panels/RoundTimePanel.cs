using System;
using Capy.Engine.Hints;
using Exiled.API.Features;
using PlayerRoles;

namespace Capy.Engine.Hud.Panels;

/// <summary>
/// Панель времени раунда с цветовым выделением роли игрока.
/// </summary>
public sealed class RoundTimePanel : HudPanel
{
    public RoundTimePanel(Player player) : base(player) { }

    public override void Update()
    {
        if (!IsConnected || !Round.IsStarted)
        {
            SetText(string.Empty, HintZone.UpperRight);
            return;
        }

        var elapsed = Round.ElapsedTime;
        string duration = elapsed.TotalHours >= 1
            ? elapsed.ToString(@"hh\:mm\:ss")
            : elapsed.ToString(@"mm\:ss");

        string color = GetRoleColor(Player.Role.Type);
        string text = $"<color=#b8b8b8>Раунд: </color><color={color}><b>{duration}</b></color>";

        SetText(text, HintZone.UpperRight, fontSize: 18, tag: "hud_round_time");
    }

    private static string GetRoleColor(RoleTypeId role)
    {
        return role switch
        {
            RoleTypeId.ClassD => "#ffa500",
            RoleTypeId.Scientist => "#ffce1b",
            RoleTypeId.FacilityGuard => "#898989",
            RoleTypeId.NtfCaptain or RoleTypeId.NtfSergeant or RoleTypeId.NtfSpecialist or RoleTypeId.NtfPrivate => "#6d9ff7",
            RoleTypeId.ChaosConscript or RoleTypeId.ChaosRifleman or RoleTypeId.ChaosMarauder or RoleTypeId.ChaosRepressor => "#608f38",
            RoleTypeId.Scp049 or RoleTypeId.Scp079 or RoleTypeId.Scp096 or RoleTypeId.Scp106 or RoleTypeId.Scp173 or RoleTypeId.Scp939 or RoleTypeId.Scp3114 => "#ff4444",
            _ => "#ffa94e"
        };
    }
}
