using System.Collections.Generic;
using Capy.Engine.CustomItems.Base;
using Capy.Engine.CustomItems.Manager;
using Capy.Engine.DevTools.Hud;
using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Models;
using Capy.Engine.Hints.Utilities;
using Exiled.API.Features;
using UnityEngine;
using Hint = Capy.Engine.Hints.Models.Hint;

namespace Capy.Engine.Hud.Panels;

public class ItemHudPanel : HudPanel
{
    private string _lastText = string.Empty;

    public ItemHudPanel(Player player) : base(player) { }

    protected override void CreateHint(PlayerDisplay display)
    {
        Vector2 pos = HudLayout.GetDynamicStatsPosition(Player);

        Hint = new Hint
        {
            Text = string.Empty,
            FontSize = 22,
            Alignment = HintAlignment.Left,
            XCoordinate = pos.x,
            YCoordinate = pos.y,
            YCoordinateAlign = HintVerticalAlign.Middle,
            Layer = HintLayer.Notification,
            Priority = 10,
            Tag = "custom_item_hud",
            SyncSpeed = HintSyncSpeed.Fast
        };

        display.AddHint(Hint);
    }

    public override void Update()
    {
        if (!IsPlayerConnected || !Player.IsAlive || Player.CurrentItem == null)
        {
            if (!string.IsNullOrEmpty(_lastText))
            {
                SetText(string.Empty);
                _lastText = string.Empty;
            }
            return;
        }

        CustomItem? customItem = CustomItemsManager.Items.FirstOrDefault(i => i.IsTracked(Player.CurrentItem));
        if (customItem == null)
        {
            if (!string.IsNullOrEmpty(_lastText))
            {
                SetText(string.Empty);
                _lastText = string.Empty;
            }
            return;
        }

        if (!EnsureHint()) return;

        Vector2 pos = HudLayout.GetDynamicStatsPosition(Player);
        Hint!.XCoordinate = pos.x;
        Hint.YCoordinate = pos.y;

        string hex = customItem.ColorHex?.TrimStart('#') ?? "FFA500";
        string text = $"<size=22><color=#{hex}><b>[{customItem.Name}]</b></color></size>";

        if (text != _lastText)
        {
            SetText(text);
            _lastText = text;
        }
    }
}
