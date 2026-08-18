using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Capy.API.DiscordBridge;

public sealed class BridgeCommandResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
}

internal sealed class BridgeCommandSender : CommandSender
{
    private static readonly Regex RichTextTag = new("<[^>]*>", RegexOptions.Compiled);

    private readonly string _commandName;
    private readonly List<string> _replies = new();
    private bool? _success;

    public BridgeCommandSender(string discordUserId, string discordUserName, string commandName)
    {
        string safeId = SanitizeIdentity(discordUserId, "unknown");
        SenderId = $"discord:{safeId}";
        Nickname = "Discord: " + SanitizeIdentity(discordUserName, safeId);
        _commandName = commandName ?? string.Empty;
    }

    public override string SenderId { get; }

    public override string Nickname { get; }

    public override ulong Permissions => ulong.MaxValue;

    public override byte KickPower => byte.MaxValue;

    public override bool FullPermissions => true;

    public override void RaReply(string text, bool success, bool logToConsole, string overrideDisplay)
    {
        _success = success;
        AddReply(text);
    }

    public override void Print(string text) => AddReply(text);

    public override bool Available() => true;

    public BridgeCommandResult Complete(string? directResponse)
    {
        string output;
        if (_replies.Count > 0)
        {
            output = string.Join("\n", _replies);
        }
        else if (!string.IsNullOrWhiteSpace(directResponse))
        {
            output = CleanResponse(directResponse);
        }
        else
        {
            output = "Команда обработана без текстового ответа.";
        }

        return new BridgeCommandResult
        {
            Success = _success ?? true,
            Output = output
        };
    }

    private void AddReply(string? text)
    {
        string cleaned = CleanResponse(text);
        if (string.IsNullOrWhiteSpace(cleaned))
            return;

        if (_replies.Count == 0 || !_replies[_replies.Count - 1].Equals(cleaned, StringComparison.Ordinal))
            _replies.Add(cleaned);
    }

    private string CleanResponse(string? text)
    {
        string value = RichTextTag.Replace(text ?? string.Empty, string.Empty).Trim();
        int separator = value.IndexOf('#');
        if (separator > 0)
        {
            string prefix = value.Substring(0, separator);
            if (prefix.Equals(_commandName, StringComparison.OrdinalIgnoreCase) ||
                prefix.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(separator + 1).TrimStart();
            }
        }

        return value;
    }

    private static string SanitizeIdentity(string? value, string fallback)
    {
        string sanitized = (value ?? string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Replace("\0", string.Empty)
            .Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
    }
}
