using System;
using System.Collections.Generic;
using System.Linq;
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

/// <summary>
/// Нативная панель HUD над полоской HP для отображения способностей предметов.
/// Обновляется синхронно в общем цикле PlayerHud без мерцания.
/// </summary>
public class ItemHudPanel : HudPanel
{
    private string _lastText = string.Empty;

    /// <summary>
    /// Внешний провайдер текста для панели предметов (например, для свободных наблюдателей).
    /// </summary>
    public static Func<Player, string?>? ExternalItemHudProvider { get; set; }

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

        string text = string.Empty;

        // 1. Проверяем внешний провайдер (Vanish монетка / карта)
        if (ExternalItemHudProvider != null)
        {
            try
            {
                text = ExternalItemHudProvider.Invoke(Player) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Log.Error($"[ItemHudPanel] Ошибка в ExternalItemHudProvider: {ex}");
            }
        }

        // 2. Если внешний провайдер ничего не вернул — проверяем стандартные кастомные предметы
        if (string.IsNullOrEmpty(text))
        {
            CustomItem? customItem = CustomItemsManager.Items.FirstOrDefault(i => i.IsTracked(Player.CurrentItem));
            if (customItem != null)
            {
                string hex = customItem.ColorHex?.TrimStart('#') ?? "FFA500";
                text = $"<size=22><color=#{hex}><b>[{customItem.Name}]</b></color></size>";
            }
        }

        if (string.IsNullOrEmpty(text))
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
        if (Hint != null)
        {
            Hint.XCoordinate = pos.x;
            Hint.YCoordinate = pos.y;
        }

        if (text != _lastText)
        {
            SetText(text);
            _lastText = text;
        }
    }
}
