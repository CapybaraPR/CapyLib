using System.Linq;
using Exiled.API.Features;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;

namespace Capy.Engine.DevTools.Hud;

public static class HudLayout {
    private const float ReferenceAspectRatio = 16f / 9f;

    private const float MaxLeftReference = (-540f * ReferenceAspectRatio) + 622f;
    private const float MaxRightReference = (540f * ReferenceAspectRatio) - 104f;

    public static AspectRatio GetAspectRatio(Player player) {
        float aspectRatio = player.ReferenceHub.aspectRatioSync.AspectRatio;
        if (Mathf.Approximately(aspectRatio, 16.0f / 9.0f)) return AspectRatio.SixteenToNine;
        if (Mathf.Approximately(aspectRatio, 4.0f / 3.0f)) return AspectRatio.FourToThree;
        if (Mathf.Approximately(aspectRatio, 16.0f / 10.0f)) return AspectRatio.SixteenToTen;
        if (Mathf.Approximately(aspectRatio, 21.0f / 9.0f)) return AspectRatio.TwentyOneToNine;

        return AspectRatio.Unknown;
    }

    public static float GetMaxLeftX(Player player) {
        float aspectRatio = player.ReferenceHub.aspectRatioSync.AspectRatio;
        return (-540f * aspectRatio) + 622f;
    }

    public static float GetMaxRightX(Player player) {
        float aspectRatio = player.ReferenceHub.aspectRatioSync.AspectRatio;
        return (540f * aspectRatio) - 104f;
    }

    public static float FromReferenceX(Player player, float referenceX) {
        float maxLeftActual = GetMaxLeftX(player);
        float maxRightActual = GetMaxRightX(player);
        float t = (referenceX - MaxLeftReference) / (MaxRightReference - MaxLeftReference);
        return maxLeftActual + t * (maxRightActual - maxLeftActual);
    }

    public static Vector2 GetDynamicStatsPosition(Player player) {
        float offset = 0f;
        Player target = player;

        if (!player.IsAlive) {
            Player? spectating = Player.List.FirstOrDefault(p => p.CurrentSpectatingPlayers.Contains(player));
            if (spectating != null) {
                target = spectating;
            }
            else {
                offset += 50f;
            }
        }

        StaminaStat stamina = target.ReferenceHub.playerStats.GetModule<StaminaStat>();
        if (stamina != null && stamina.CurValue < stamina.MaxValue) offset += 32f;

        if (target.HumeShield > 0) offset += 42f;

        if (target.ArtificialHealth > 0) offset += 42f;

        if (target.Role.Type == RoleTypeId.Scp106) offset += 42f;

        return new Vector2(-295f, 980f - offset);
    }
}
