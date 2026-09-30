using HelpdeskAPI.Models;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Data;

public class TicketRepository : ITicketRepository
{
    private static List<Ticket> _tickets = new()
    {
        new() { Id = 1, TicketNumber = "TKT-20260101-1001", Title = "Login Issue", Description = "Can&#39;t login", UserName = "john.doe", Status = "Open", Priority = "High", Category = "Access", Channel = "Web", CreatedAt = DateTime.UtcNow.AddHours(-5), LastActivityAt = DateTime.UtcNow.AddHours(-1), ResponseDueAt = DateTime.UtcNow.AddMinutes(30), ResolutionDueAt = DateTime.UtcNow.AddHours(20) },
        new() { Id = 2, TicketNumber = "TKT-20260102-1002", Title = "Printer not working", Description = "HP printer offline", UserName = "jane.smith", Status = "InProgress", Priority = "Medium", Category = "Hardware", Channel = "Email", AssignedTo = "agent1", CreatedAt = DateTime.UtcNow.AddDays(-1), LastActivityAt = DateTime.UtcNow.AddHours(-3), ResponseDueAt = DateTime.UtcNow.AddHours(-2), ResolutionDueAt = DateTime.UtcNow.AddDays(2), SlaBreached = true }
    };
    private static int _nextId = 3;

    private static List<TicketReply> _replies = new()
    {
        new() { Id = 1, TicketId = 2, Author = "agent1", Body = "Checked print queue. Parts ordered.", IsInternal = false },
        new() { Id = 2, TicketId = 2, Author = "agent1", Body = "Customer has old driver. Follow up tomorrow.", IsInternal = true },
    };
    private static int _nextReply = 3;

    private static List<TicketAttachment> _files = new();
    private static int _nextFile = 1;

    public static readonly string[] AllowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".pdf", ".txt", ".docx", ".xlsx", ".csv" };
    public const long MaxFileBytes = 10 * 1024 * 1024;

    public Task<List<Ticket>> GetAllAsync() => Task.FromResult(_tickets.OrderByDescending(t => t.CreatedAt).ToList());
    public Task<Ticket?> GetByIdAsync(int id) => Task.FromResult(_tickets.FirstOrDefault(t => t.Id == id));
    public Task<Ticket?> GetByNumberAsync(string n) => Task.FromResult(_tickets.FirstOrDefault(t => t.TicketNumber.Equals(n, StringComparison.OrdinalIgnoreCase)));

    public Task<Ticket> CreateAsync(Ticket ticket)
    {
        ticket.Id = _nextId++;
        ticket.TicketNumber = WorkflowService.NextTicketNumber();
        ticket.Title = SanitizerService.Sanitize(ticket.Title, 200);
        ticket.Description = SanitizerService.Sanitize(ticket.Description, 10000);
        if (!WorkflowService.Statuses.Contains(ticket.Status)) ticket.Status = "New";
        ticket.CreatedAt = DateTime.UtcNow;
        ticket.LastActivityAt = DateTime.UtcNow;
        SlaDefaults.ApplyTo(ticket);
        _tickets.Add(ticket);
        return Task.FromResult(ticket);
    }

    public Task<bool> UpdateAsync(Ticket ticket)
    {
        var existing = _tickets.FirstOrDefault(t => t.Id == ticket.Id);
        if (existing == null) return Task.FromResult(false);

        existing.Title = SanitizerService.Sanitize(ticket.Title, 200);
        existing.Description = SanitizerService.Sanitize(ticket.Description, 10000);
        existing.Status = ticket.Status;
        existing.Priority = ticket.Priority;
        existing.Category = ticket.Category;
        existing.AssignedTo = ticket.AssignedTo;
        existing.AiOverridden = ticket.AiOverridden;
        if (ticket.AiOverridden) { existing.AiCategory = null; existing.AiPriority = null; }
        existing.UpdatedAt = DateTime.UtcNow;
        existing.LastActivityAt = DateTime.UtcNow;
        if (existing.FirstResponseAt == null && ticket.Status != "New" && ticket.Status != "Open") existing.FirstResponseAt = DateTime.UtcNow;
        if (ticket.Status == "Resolved") existing.ResolvedAt ??= DateTime.UtcNow;
        if (ticket.Status == "Closed") existing.ClosedAt ??= DateTime.UtcNow;
        if (ticket.Status == "Open" && existing.ClosedAt != null) { /* reopen keeps history */ }
        SlaDefaults.ApplyTo(existing);
        if (existing.ResponseDueAt != null && existing.ResolutionDueAt != null)
            existing.SlaBreached = DateTime.UtcNow > existing.ResponseDueAt || DateTime.UtcNow > existing.ResolutionDueAt;

        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int id)
    {
        var ticket = _tickets.FirstOrDefault(t => t.Id == id);
        if (ticket == null) return Task.FromResult(false);
        _tickets.Remove(ticket);
        return Task.FromResult(true);
    }

    public Task<List<Ticket>> GetUserTicketsAsync(string username) =>
        Task.FromResult(_tickets.Where(t => t.UserName == username).OrderByDescending(t => t.CreatedAt).ToList());

    public Task<List<Ticket>> GetAdminTicketsAsync() => Task.FromResult(_tickets);

    public Task<List<Ticket>> SearchAsync(string? q, string? status, string? priority, string? category, string? assignee, DateTime? from, DateTime? to)
    {
        var query = _tickets.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(t => (t.TicketNumber + " " + t.Title + " " + t.Description).Contains(q, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status) && status != "All") query = query.Where(t => t.Status == status);
        if (!string.IsNullOrWhiteSpace(priority) && priority != "All") query = query.Where(t => t.Priority == priority);
        if (!string.IsNullOrWhiteSpace(category) && category != "All") query = query.Where(t => t.Category == category);
        if (!string.IsNullOrWhiteSpace(assignee) && assignee != "All") query = query.Where(t => (t.AssignedTo ?? string.Empty) == assignee);
        if (from != null) query = query.Where(t => t.CreatedAt >= from);
        if (to != null) query = query.Where(t => t.CreatedAt <= to);
        return Task.FromResult(query.OrderByDescending(t => t.CreatedAt).ToList());
    }

    public Task<List<TicketReply>> GetRepliesAsync(int ticketId, bool includeInternal, string requester, string role)
    {
        var list = _replies.Where(r => r.TicketId == ticketId).OrderBy(r => r.CreatedAt).ToList();
        if (includeInternal && (role == "Agent" || role == "Admin")) return Task.FromResult(list);
        // Customers see public replies plus their own internal? No, internal hidden.
        return Task.FromResult(list.Where(r => !r.IsInternal).ToList());
    }

    public Task<TicketReply> AddReplyAsync(TicketReply reply)
    {
        reply.Id = _nextReply++;
        reply.Author = SanitizerService.Sanitize(reply.Author, 100);
        reply.Body = SanitizerService.Sanitize(reply.Body, 10000);
        reply.CreatedAt = DateTime.UtcNow;
        _replies.Add(reply);
        var t = _tickets.FirstOrDefault(x => x.Id == reply.TicketId);
        if (t != null)
        {
            t.LastActivityAt = DateTime.UtcNow;
            t.UpdatedAt = DateTime.UtcNow;
            if (t.FirstResponseAt == null && reply.Author != t.UserName) t.FirstResponseAt = DateTime.UtcNow;
        }
        return Task.FromResult(reply);
    }

    public Task<List<TicketAttachment>> GetAttachmentsAsync(int ticketId) =>
        Task.FromResult(_files.Where(f => f.TicketId == ticketId).ToList());

    public Task<TicketAttachment?> GetAttachmentAsync(int attachmentId) =>
        Task.FromResult(_files.FirstOrDefault(f => f.Id == attachmentId));

    public Task<TicketAttachment> AddAttachmentAsync(TicketAttachment attachment)
    {
        attachment.Id = _nextFile++;
        attachment.UploadedAt = DateTime.UtcNow;
        _files.Add(attachment);
        var t = _tickets.FirstOrDefault(x => x.Id == attachment.TicketId);
        if (t != null) t.LastActivityAt = DateTime.UtcNow;
        return Task.FromResult(attachment);
    }

    public Task AutoCloseIdleAsync()
    {
        foreach (var t in _tickets.Where(t => WorkflowService.ShouldAutoClose(t.Status, t.LastActivityAt)))
        {
            t.Status = "Closed";
            t.ClosedAt ??= DateTime.UtcNow;
            t.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task<List<Ticket>> GetBreachedAsync() =>
        Task.FromResult(_tickets.Where(t => t.SlaBreached && t.Status != "Closed").ToList());
}
