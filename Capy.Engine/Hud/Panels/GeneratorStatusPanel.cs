using System.Linq;
using Capy.Engine.Hints;
using Exiled.API.Features;

namespace Capy.Engine.Hud.Panels;

/// <summary>
/// Панель статуса включенных генераторов в комплексе.
/// </summary>
public sealed class GeneratorStatusPanel : HudPanel
{
    public GeneratorStatusPanel(Player player) : base(player) { }

    public override void Update()
    {
        if (!IsConnected || !Round.IsStarted)
        {
            SetText(string.Empty, HintZone.UpperRight);
            return;
        }

        var generators = Generator.List.ToList();
        if (generators.Count == 0)
        {
            SetText(string.Empty, HintZone.UpperRight);
            return;
        }

        int engaged = generators.Count(g => g.IsEngaged);
        int total = generators.Count;

        // Показываем, если хотя бы один генератор включен или в процессе
        if (engaged > 0)
        {
            string color = engaged == total ? "#22c55e" : "#38bdf8";
            string text = $"<color={color}>⚙️ <b>Генераторы: {engaged}/{total}</b></color>";
            SetText(text, HintZone.UpperRight, fontSize: 17, tag: "hud_generators");
        }
        else
        {
            SetText(string.Empty, HintZone.UpperRight);
        }
    }
}
