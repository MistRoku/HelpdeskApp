using HelpdeskAPI.Models;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Services;

public class NotificationService
{
    private static readonly List<NotificationItem> _items = new();
    private static readonly List<AuditEntry> _audit = new();
    private static int _nextNotif = 1;
    private static int _nextAudit = 1;
    private static readonly object _lock = new();

    public event Action<NotificationItem>? OnCreated;

    public NotificationItem Notify(string user, string title, string body, int? ticketId = null)
    {
        var n = new NotificationItem
        {
            Id = 0, UserName = SanitizerService.Sanitize(user, 100),
            Title = SanitizerService.Sanitize(title, 200),
            Body = SanitizerService.Sanitize(body, 1000),
            TicketId = ticketId
        };
        lock (_lock) { n.Id = _nextNotif++; _items.Add(n); }
        OnCreated?.Invoke(n);
        return n;
    }

    public void Broadcast(string title, string body, int? ticketId = null)
    {
        foreach (var u in new[] { "agent1", "admin" }) Notify(u, title, body, ticketId);
    }

    public List<NotificationItem> ForUser(string user) =>
        _items.Where(n => n.UserName.Equals(user, StringComparison.OrdinalIgnoreCase))
              .OrderByDescending(n => n.CreatedAt).Take(50).ToList();

    public bool MarkRead(int id, string user)
    {
        var n = _items.FirstOrDefault(x => x.Id == id && x.UserName.Equals(user, StringComparison.OrdinalIgnoreCase));
        if (n == null) return false;
        n.IsRead = true;
        return true;
    }

    public void Audit(int? ticketId, string actor, string action)
    {
        lock (_lock) _audit.Add(new AuditEntry { Id = _nextAudit++, TicketId = ticketId, Actor = actor, Action = action });
    }

    public List<AuditEntry> Recent(int take = 30) => _audit.OrderByDescending(a => a.CreatedAt).Take(take).ToList();
}
