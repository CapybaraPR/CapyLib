using System;
using System.Linq;
using System.Text;
using Capy.Engine.Hints;
using Exiled.API.Features;
using PlayerRoles;

namespace Capy.Engine.Hud.Panels;

/// <summary>
/// Нижняя информационная панель для наблюдателей (информация об игроке, за которым смотрят).
/// </summary>
public sealed class SpectatorBottomPanel : HudPanel
{
    public SpectatorBottomPanel(Player player) : base(player) { }

    public override void Update()
    {
        if (!IsConnected || Player.IsAlive || Player.Role.Type != RoleTypeId.Spectator || !Round.IsStarted)
        {
            SetText(string.Empty, HintZone.BottomCenter);
            return;
        }

        var target = HudModule.GetSpectatedTarget(Player);

        var sb = new StringBuilder();

        if (target != null && target.IsConnected && target.IsAlive)
        {
            string roleName = target.Role.Type.ToString();
            string hp = $"<color=#ff4444>❤️ {(int)target.Health}/{(int)target.MaxHealth} HP</color>";

            if (target.ArtificialHealth > 0)
                hp += $" <color=#38bdf8>🛡️ {(int)target.ArtificialHealth} AHP</color>";

            if (target.HumeShield > 0)
                hp += $" <color=#a855f7>⚡ {(int)target.HumeShield} HS</color>";

            string heldItemName = target.CurrentItem != null ? target.CurrentItem.Type.ToString() : "Кулаки";

            sb.AppendLine($"<color=#b8b8b8>Наблюдаю за:</color> <color=#ffa94e><b>{target.Nickname}</b></color> <color=#999999>({roleName})</color>");
            sb.AppendLine($"{hp}  |  <color=#a3e635>📦 {heldItemName}</color>");
        }
        else
        {
            int aliveCount = Player.List.Count(p => p.IsAlive);
            int specCount = Player.List.Count(p => !p.IsAlive && p.Role.Type == RoleTypeId.Spectator);
            sb.AppendLine($"<color=#ffa94e><b>РЕЖИМ НАБЛЮДАТЕЛЯ</b></color>");
            sb.AppendLine($"<color=#c2c2c2>Живых: <color=#a3e635>{aliveCount}</color>  |  Зрителей: <color=#ffa94e>{specCount}</color></color>");
        }

        SetText(sb.ToString().TrimEnd(), HintZone.BottomCenter, fontSize: 19, tag: "hud_spectator_bottom");
    }
}
