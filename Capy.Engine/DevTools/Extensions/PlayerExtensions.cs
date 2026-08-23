using System.Collections.Generic;
using System.Linq;
using InventorySystem.Items;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using Capy.Engine.FakeSync;
using UnityEngine;

namespace Capy.Engine.DevTools.Extensions;

public static class PlayerExtensions {
    public static void AddAhp(this Player player, float amount, float decay = 1.2f, float efficacy = 0.7f, float sustain = 0f, bool persistant = false) {
        if (amount < 0f)
            return;

        player.ReferenceHub.playerStats.GetModule<AhpStat>().ServerAddProcess(amount, player.MaxArtificialHealth, decay, efficacy, sustain, persistant);
    }

    public static void ThrewItem(this Player player, Item item) {
        player.DropItem(item);
    }

    public static void SetScale(this Player player, Vector3 value, bool isFake = false) {
        Vector3 original = player.Scale;
        if (value == original && !isFake)
            return;

        if (isFake) {
            SetFakeScale(player, Player.List.Where(x => x != player), value);
            return;
        }

        player.Scale = value;
        if (value == Vector3.one)
            return;

        if (player.ReferenceHub.roleManager.CurrentRole is not IFpcRole fpcRole)
            return;

        float halfHeight = fpcRole.FpcModule.CharController.height / 2;
        float tpY = value.y < -0.1f ? value.y * -1f : value.y - halfHeight;
        player.Position += new Vector3(0f, tpY, 0f);
    }

    public static void SetFakeGravity(this Player player, Player target, Vector3 gravity) {
        player.ReferenceHub.connectionToClient.Send<SyncedGravityMessages.GravityMessage>(new(gravity, target.ReferenceHub));
    }

    public static void SetFakeGravity(this Player player, IEnumerable<Player> targets, Vector3 gravity) {
        foreach (Player target in targets) {
            player.SetFakeGravity(target, gravity);
        }
    }

    public static void SetFakeScale(this Player player, Player target, Vector3 scale) {
        player.ReferenceHub.connectionToClient.Send<SyncedScaleMessages.ScaleMessage>(new(scale, target.ReferenceHub));
    }

    public static void SetFakeScale(this Player player, IEnumerable<Player> targets, Vector3 scale) {
        foreach (Player target in targets) {
            player.SetFakeScale(target, scale);
        }
    }

    public static void SetFakeBadgeText(this Player player, Player target, string badgeText) {
        target.SendFakeSyncVar(player.ReferenceHub.serverRoles, 1, badgeText);
    }

    public static void SetFakeBadgeColor(this Player player, Player target, string color) {
        target.SendFakeSyncVar(player.ReferenceHub.serverRoles, 2, color);
    }

    public static void SetFakeViewRange(this Player player, Player target, float viewRange) {
        target.SendFakeSyncVar(player.ReferenceHub.nicknameSync, 1, viewRange);
    }

    public static void SetFakeCustomInfo(this Player player, Player target, string customInfo) {
        target.SendFakeSyncVar(player.ReferenceHub.nicknameSync, 2, customInfo);
    }

    public static void SetFakeInfoArea(this Player player, Player target, PlayerInfoArea playerInfoArea) {
        target.SendFakeSyncVar(player.ReferenceHub.nicknameSync, 4, playerInfoArea);
    }

    public static void SetFakeNick(this Player player, Player target, string nick) {
        target.SendFakeSyncVar(player.ReferenceHub.nicknameSync, 8, nick);
    }

    public static void SetFakeDisplayName(this Player player, Player target, string displayName) {
        target.SendFakeSyncVar(player.ReferenceHub.nicknameSync, 16, displayName);
    }

    public static void SetFakeMaxPlayers(this Player player, Player target, ushort maxPlayer) {
        target.SendFakeSyncVar(player.ReferenceHub.characterClassManager, 2, maxPlayer);
    }

    public static void SetFakeCurrentItem(this Player player, Player target, ItemIdentifier item) {
        target.SendFakeSyncVar(player.ReferenceHub.inventory, 1, item);
    }
}