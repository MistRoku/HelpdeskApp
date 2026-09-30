namespace HelpdeskAPI.Models;

public class Ticket
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty; // format TKT-YYYYMMDD-XXXX
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    // New, Open, InProgress, Pending, Resolved, Closed
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent
    public string Category { get; set; } = "General"; // Network, Software, Hardware, Access, Billing, Other, General
    public string Channel { get; set; } = "Web"; // Web, Email, Chat, Phone
    public string UserName { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    // AI assistance (rule based, capped). Stored so agents can review and override.
    public string? AiCategory { get; set; }
    public string? AiPriority { get; set; }
    public double AiConfidence { get; set; }
    public bool AiOverridden { get; set; }

    // SLA tracking
    public DateTime? ResponseDueAt { get; set; }
    public DateTime? ResolutionDueAt { get; set; }
    public bool SlaBreached { get; set; }
}
