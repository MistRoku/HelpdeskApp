namespace HelpdeskAPI.Models;

public class Article
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string Tags { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft"; // Draft, Published, Archived
    public string Author { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int HelpfulCount { get; set; }
    public int UnhelpfulCount { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
}

public class SlaPolicy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public int ResponseMinutes { get; set; }
    public int ResolutionMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SlaBreach
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string BreachType { get; set; } = string.Empty; // Response, Resolution
    public DateTime BreachedAt { get; set; } = DateTime.UtcNow;
    public string? EscalatedTo { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class CannedResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public bool IsShared { get; set; }
    public string Owner { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FeedbackEntry
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string Type { get; set; } = "CSAT"; // CSAT, NPS
    public int Score { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
