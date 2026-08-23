using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Capy.Engine.Hints.Utilities;

/// <summary>
/// Fluent Builder и утилита валидации/автоматического закрытия RichText тегов Unity.
/// </summary>
public static class HintStringBuilder
{
    private static readonly Regex OpeningTagRegex = new(@"<([a-zA-Z0-9=#\.\-]+)[^>]*>", RegexOptions.Compiled);
    private static readonly Regex ClosingTagRegex = new(@"</([a-zA-Z0-9]+)>", RegexOptions.Compiled);

    public static StringBuilder WithColor(this StringBuilder sb, string colorHex, string text)
    {
        return sb.AppendFormat("<color=#{0}>{1}</color>", colorHex.TrimStart('#'), text);
    }

    public static StringBuilder WithBold(this StringBuilder sb, string text)
    {
        return sb.AppendFormat("<b>{0}</b>", text);
    }

    public static StringBuilder WithItalic(this StringBuilder sb, string text)
    {
        return sb.AppendFormat("<i>{0}</i>", text);
    }

    public static StringBuilder WithSize(this StringBuilder sb, int size, string text)
    {
        return sb.AppendFormat("<size={0}>{1}</size>", size, text);
    }

    public static StringBuilder WithVOffset(this StringBuilder sb, float voffset, string text)
    {
        return sb.AppendFormat("<voffset={0:0.#}>{1}</voffset>", voffset, text);
    }

    public static StringBuilder WithPos(this StringBuilder sb, float pos, string text)
    {
        return sb.AppendFormat("<pos={0:0.#}>{1}</pos>", pos, text);
    }

    /// <summary>
    /// Автоматически найти незакрытые RichText теги (<b>, <color>, <size>, <align> и др.)
    /// и дописать необходимые закрывающие теги в конец строки.
    /// </summary>
    public static string RepairUnclosedTags(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;

        Stack<string> openTags = new Stack<string>();
        MatchCollection matches = Regex.Matches(html, @"</?([a-zA-Z0-9]+)(?:=[^>]*)?>");

        foreach (Match match in matches)
        {
            string fullTag = match.Value;
            string tagName = match.Groups[1].Value.ToLowerInvariant();

            // Пропускаем теги-самозакрыватели или одиночные теги (например, <br>, <a>, <line-height=0>)
            if (tagName == "br" || tagName == "line-height" || tagName == "voffset" || tagName == "pos")
                continue;

            if (fullTag.StartsWith("</"))
            {
                if (openTags.Count > 0 && openTags.Peek() == tagName)
                {
                    openTags.Pop();
                }
            }
            else
            {
                openTags.Push(tagName);
            }
        }

        if (openTags.Count == 0) return html;

        StringBuilder sb = new StringBuilder(html);
        while (openTags.Count > 0)
        {
            string tag = openTags.Pop();
            sb.AppendFormat("</{0}>", tag);
        }

        return sb.ToString();
    }
}
