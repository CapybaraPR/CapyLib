using System;
using System.Collections.Generic;
using System.Linq;
using Capy.Engine.Hints;
using Capy.Engine.Hints.Enum;
using Capy.Engine.Hints.Extensions;
using Exiled.API.Features;
using UnityEngine;

namespace Capy.Engine.Studio.Core;

/// <summary>
/// Компонент функционального портала-телепорта в CapyStudio.
/// </summary>
public sealed class TeleportComponent : MonoBehaviour
{
    private static readonly List<TeleportComponent> AllTeleports = new();

    public string TeleportId { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    public List<string> Targets { get; set; } = new();
    public float Cooldown { get; set; } = 2.0f;
    public DateTime NextUseTime { get; set; } = DateTime.MinValue;

    private void Awake()
    {
        lock (AllTeleports)
            AllTeleports.Add(this);
    }

    private void OnDestroy()
    {
        lock (AllTeleports)
            AllTeleports.Remove(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = Player.Get(other.gameObject);
        if (player == null || !player.IsAlive)
            return;

        if (DateTime.UtcNow < NextUseTime)
            return;

        TeleportComponent? destination = GetDestination();
        if (destination == null)
            return;

        DateTime next = DateTime.UtcNow.AddSeconds(Cooldown);
        NextUseTime = next;
        destination.NextUseTime = next;

        // Перемещаем игрока
        Vector3 targetPos = destination.transform.position + Vector3.up * 0.1f;
        player.Position = targetPos;
        player.Rotation = destination.transform.rotation;

        player.ShowZoneHint(HintZone.Notification, "<color=#38bdf8>🌀 <b>Телепортация!</b></color>", 1.5f, "capy_tp", 20);
    }

    private TeleportComponent? GetDestination()
    {
        lock (AllTeleports)
        {
            if (Targets.Count > 0)
            {
                var matching = AllTeleports.Where(t => t != this && Targets.Contains(t.TeleportId, StringComparer.OrdinalIgnoreCase)).ToList();
                if (matching.Count > 0)
                    return matching[UnityEngine.Random.Range(0, matching.Count)];
            }

            // Если список целей пуст, ищем любой другой свободный телепорт
            var others = AllTeleports.Where(t => t != this).ToList();
            if (others.Count > 0)
                return others[UnityEngine.Random.Range(0, others.Count)];

            return null;
        }
    }
}
