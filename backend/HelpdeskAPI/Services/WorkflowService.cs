namespace HelpdeskAPI.Services;

/// <summary>
/// Ticket number format TKT-YYYYMMDD-XXXX plus status transition rules.
/// Flow: New to Open to InProgress to Pending to Resolved to Closed.
/// Reopen allowed within 7 days of closure. Auto close after 14 days idle.
/// </summary>
public static class WorkflowService
{
    private static int _seq = 1000;
    private static readonly object _lock = new();
    private static string _day = DateTime.UtcNow.ToString("yyyyMMdd");

    public static readonly string[] Statuses = new[] { "New", "Open", "InProgress", "Pending", "Resolved", "Closed" };

    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        ["New"] = new[] { "Open", "Closed" },
        ["Open"] = new[] { "InProgress", "Pending", "Resolved", "Closed" },
        ["InProgress"] = new[] { "Pending", "Resolved", "Closed", "Open" },
        ["Pending"] = new[] { "Open", "InProgress", "Resolved" },
        ["Resolved"] = new[] { "Closed", "Open" },
        ["Closed"] = new[] { "Open" }, // reopen only, checked against 7 day rule
    };

    public static string NextTicketNumber()
    {
        lock (_lock)
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            if (today != _day) { _day = today; _seq = 1000; }
            _seq++;
            return "TKT-" + today + "-" + _seq;
        }
    }

    public static bool CanTransition(string from, string to)
    {
        if (from == to) return true;
        return Allowed.TryGetValue(from, out var next) && next.Contains(to);
    }

    public static bool CanReopen(DateTime? closedAt)
    {
        if (closedAt == null) return false;
        return (DateTime.UtcNow - closedAt.Value).TotalDays <= 7;
    }

    public static bool ShouldAutoClose(string status, DateTime lastActivity)
    {
        if (status == "Closed") return false;
        return (DateTime.UtcNow - lastActivity).TotalDays >= 14;
    }
}
