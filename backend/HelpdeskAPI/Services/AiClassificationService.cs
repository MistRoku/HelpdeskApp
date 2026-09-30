using System.Text.RegularExpressions;

namespace HelpdeskAPI.Services;

public record AiClassification(string Category, string Priority, double Confidence, string? SuggestedArticle, bool Blocked);

/// <summary>
/// Rule based classification with prompt injection blocking and per user daily caps.
/// Replaces an external LLM call with a deterministic, auditable baseline.
/// </summary>
public class AiClassificationService
{
    public const int DailyCapPerUser = 50;

    private static readonly Dictionary<string, int> DailyUsage = new();
    private static readonly object UsageLock = new();

    // Patterns that indicate an attempt to override system behaviour. Requests
    // containing these are refused rather than classified.
    private static readonly Regex InjectionPattern = new(
        @"(ignore\s+(all\s+)?(previous|prior|above)\s+instructions|system\s*prompt|you\s+are\s+now|disregard\s+.*instructions|reveal\s+.*prompt|jailbreak|bypass\s+.*(filter|safety|policy))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool TryCheckCap(string user, out int remaining)
    {
        var key = user + ":" + DateTime.UtcNow.ToString("yyyy-MM-dd");
        lock (UsageLock)
        {
            DailyUsage.TryGetValue(key, out var used);
            remaining = Math.Max(0, DailyCapPerUser - used);
            if (used >= DailyCapPerUser) return false;
            DailyUsage[key] = used + 1;
            remaining = Math.Max(0, DailyCapPerUser - (used + 1));
            return true;
        }
    }

    public AiClassification Classify(string subject, string description, string user)
    {
        var combined = ((subject ?? string.Empty) + " " + (description ?? string.Empty)).Trim();

        if (InjectionPattern.IsMatch(combined))
        {
            return new AiClassification("General", "Medium", 0, null, true);
        }

        var text = combined.ToLowerInvariant();
        string category = "General";
        double confidence = 0.55;
        string? article = null;

        if (ContainsAny(text, "password", "login", "sign in", "mfa", "access denied"))
        { category = "Access"; confidence = 0.88; article = "How to reset your password"; }
        else if (ContainsAny(text, "wifi", "vpn", "network", "internet", "dns"))
        { category = "Network"; confidence = 0.9; article = "Network troubleshooting checklist"; }
        else if (ContainsAny(text, "laptop", "printer", "monitor", "keyboard", "hardware"))
        { category = "Hardware"; confidence = 0.86; article = "Report a hardware fault"; }
        else if (ContainsAny(text, "invoice", "billing", "payment", "refund", "quote"))
        { category = "Billing"; confidence = 0.89; article = "Billing and invoice queries"; }
        else if (ContainsAny(text, "install", "software", "app crash", "update", "license"))
        { category = "Software"; confidence = 0.84; article = "Request software installation"; }

        string priority = "Medium";
        if (ContainsAny(text, "outage", "down", "breach", "urgent", "payroll blocked", "cannot work"))
            priority = "Urgent";
        else if (ContainsAny(text, "deadline", "asap", "blocked", "frustrated", "angry"))
            priority = "High";
        else if (ContainsAny(text, "question", "how do i", "request", "please advise"))
            priority = "Low";

        if (combined.Length < 12) confidence = Math.Min(confidence, 0.5);

        return new AiClassification(category, priority, Math.Round(confidence, 2), article, false);
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        foreach (var t in terms) if (text.Contains(t)) return true;
        return false;
    }
}
