using System.Text;
using UnityEngine;

namespace Capy.Core.Features;

public static class GradientHelper
{
    private const int MaxOutputLength = 1000;

    private struct GradientStop(float position, Color color)
    {
        public readonly float Position = position;
        public readonly Color Color = color;
    }

    public static string ApplyGradient(this string text, List<(string hex, float stop)> gradientStops)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 2 || gradientStops.Count < 2)
            return text;

        List<GradientStop> stops = new(gradientStops.Count);
        foreach ((string hex, float stop) in gradientStops)
        {
            if (!ColorUtility.TryParseHtmlString(NormalizeHex(hex), out Color color))
                throw new ArgumentException($"Invalid HEX: {hex}");

            stops.Add(new(stop, color));
        }
        stops.Sort((a, b) => a.Position.CompareTo(b.Position));

        StringBuilder sb = new(text.Length * 16);
        StringBuilder segment = new();
        string? currentHex = null;

        int len = text.Length;
        for (int i = 0; i < len; i++)
        {
            float percent = i / (float)(len - 1) * 100f;

            GradientStop left = stops[0], right = stops[stops.Count - 1];
            for (int j = 0; j < stops.Count; j++)
            {
                if (!(stops[j].Position > percent)) continue;
                right = stops[j];
                if (j > 0) left = stops[j - 1];
                break;
            }

            float t = Mathf.InverseLerp(left.Position, right.Position, percent);
            Color col = Color.Lerp(left.Color, right.Color, t);
            string hex = ColorUtility.ToHtmlStringRGB(col);

            if (!string.Equals(hex, currentHex, StringComparison.OrdinalIgnoreCase))
            {
                FlushSegment(sb, segment, currentHex);
                currentHex = hex;
            }

            segment.Append(text[i]);
        }

        FlushSegment(sb, segment, currentHex);

        string result = sb.ToString();
        return result.Length <= MaxOutputLength ? result : text;
    }

    private static void FlushSegment(StringBuilder target, StringBuilder segment, string? hex)
    {
        if (segment.Length == 0 || string.IsNullOrEmpty(hex))
            return;

        target.Append("<color=#").Append(hex).Append('>').Append(segment).Append("</color>");
        segment.Clear();
    }

    private static string NormalizeHex(string hex) => hex.StartsWith("#") ? hex : $"#{hex}";
}
