using System.Net;
using System.Text.RegularExpressions;

namespace HelpdeskAPI.Services;

/// <summary>
/// Sanitizes user supplied text before storage to prevent stored XSS.
/// Encodes HTML and strips event handlers, script blocks and javascript URIs.
/// </summary>
public static class SanitizerService
{
    private static readonly Regex ScriptBlock = new(@"<\s*script[^>]*>.*?<\s*/\s*script\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex EventHandler = new(@"\son\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex JsUri = new(@"javascript\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Sanitize(string? input, int maxLength = 10000)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var text = input.Trim();
        if (text.Length > maxLength) text = text.Substring(0, maxLength);
        text = ScriptBlock.Replace(text, string.Empty);
        text = EventHandler.Replace(text, string.Empty);
        text = JsUri.Replace(text, string.Empty);
        return WebUtility.HtmlEncode(text);
    }
}
