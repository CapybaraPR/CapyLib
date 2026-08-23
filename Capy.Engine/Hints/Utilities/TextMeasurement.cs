using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Capy.Engine.Hints.Utilities;

/// <summary>
/// Утилита оценки ширины текста и автоматического переноса строк для TextMeshPro / Unity.
/// </summary>
public static class TextMeasurement
{
    private static readonly Regex RichTextRegex = new(@"<[^>]*>", RegexOptions.Compiled);

    /// <summary>
    /// Оценить ширину одного символа при базовом размере шрифта 20.
    /// </summary>
    private static float GetCharWidthRatio(char c)
    {
        if (c >= 'A' && c <= 'Z') return 12f;
        if (c >= 'А' && c <= 'Я') return 13f;
        if (c >= 'a' && c <= 'z') return 9f;
        if (c >= 'а' && c <= 'я') return 9f;
        if (c >= '0' && c <= '9') return 10f;
        if (c == ' ' || c == '\t') return 6f;
        if (c == 'i' || c == 'l' || c == 'I' || c == '1' || c == '.' || c == ',' || c == '!' || c == ':') return 5f;
        if (c == 'W' || c == 'M' || c == 'Ш' || c == 'Щ' || c == 'Ж') return 16f;
        if (c == 'w' || c == 'm' || c == 'ш' || c == 'щ' || c == 'ж') return 13f;

        // Символы и широкие пиктограммы
        return 10f;
    }

    /// <summary>
    /// Очистить строку от RichText тегов Unity.
    /// </summary>
    public static string StripRichTextTags(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return RichTextRegex.Replace(input, string.Empty);
    }

    /// <summary>
    /// Рассчитать приблизительную ширину текста в пикселях.
    /// </summary>
    public static float EstimateTextWidth(string text, int fontSize = 20)
    {
        if (string.IsNullOrEmpty(text)) return 0f;

        string cleanText = StripRichTextTags(text);
        float scale = fontSize / 20f;
        float totalWidth = 0f;

        foreach (char c in cleanText)
        {
            if (c == '\n' || c == '\r') continue;
            totalWidth += GetCharWidthRatio(c) * scale;
        }

        return totalWidth;
    }

    /// <summary>
    /// Автоматически расставить переносы строк (\n) при превышении maxPixelWidth.
    /// </summary>
    public static string WrapText(string text, int fontSize = 20, float maxPixelWidth = 1000f)
    {
        if (string.IsNullOrEmpty(text) || maxPixelWidth <= 0f) return text;

        string[] paragraphs = text.Split('\n');
        StringBuilder result = new StringBuilder(text.Length + 16);

        for (int p = 0; p < paragraphs.Length; p++)
        {
            if (p > 0) result.Append('\n');

            string paragraph = paragraphs[p];
            if (EstimateTextWidth(paragraph, fontSize) <= maxPixelWidth)
            {
                result.Append(paragraph);
                continue;
            }

            // Разбиваем абзац по словам и переносим при необходимости
            string[] words = paragraph.Split(' ');
            float currentLineWidth = 0f;
            float scale = fontSize / 20f;

            for (int w = 0; w < words.Length; w++)
            {
                string word = words[w];
                float wordWidth = EstimateTextWidth(word, fontSize);

                if (currentLineWidth > 0f && (currentLineWidth + wordWidth + (6f * scale)) > maxPixelWidth)
                {
                    result.Append('\n');
                    currentLineWidth = 0f;
                }
                else if (w > 0 && currentLineWidth > 0f)
                {
                    result.Append(' ');
                    currentLineWidth += 6f * scale;
                }

                result.Append(word);
                currentLineWidth += wordWidth;
            }
        }

        return result.ToString();
    }
}
