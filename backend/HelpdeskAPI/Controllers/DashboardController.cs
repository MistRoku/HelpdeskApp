using Microsoft.AspNetCore.Mvc;
using HelpdeskAPI.Data;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Controllers;

[Route("api/dashboard")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly ITicketRepository _tickets;
    private readonly CatalogStore _catalog;
    private readonly NotificationService _notifications;

    public DashboardController(ITicketRepository tickets, CatalogStore catalog, NotificationService notifications)
    {
        _tickets = tickets;
        _catalog = catalog;
        _notifications = notifications;
    }

    [HttpGet("stats")]
    public async Task<ActionResult> Stats()
    {
        // Open to all callers; per-ticket endpoints enforce ownership.
        var all = await _tickets.GetAllAsync();
        var byStatus = all.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.Count());
        var open = all.Count(t => t.Status == "New" || t.Status == "Open");
        var inProgress = all.Count(t => t.Status == "InProgress");
        var resolvedToday = all.Count(t => t.ResolvedAt != null && t.ResolvedAt.Value.Date == DateTime.UtcNow.Date);
        var withResponse = all.Where(t => t.FirstResponseAt != null).ToList();
        double avgResponseMin = withResponse.Count == 0 ? 0 :
            Math.Round(withResponse.Average(t => (t.FirstResponseAt!.Value - t.CreatedAt).TotalMinutes), 1);
        var resolved = all.Where(t => t.ResolvedAt != null).ToList();
        double avgResolveHrs = resolved.Count == 0 ? 0 :
            Math.Round(resolved.Average(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours), 1);
        var byAgent = all.Where(t => t.AssignedTo != null)
            .GroupBy(t => t.AssignedTo!)
            .Select(g => new { agent = g.Key, assigned = g.Count(), resolved = g.Count(t => t.Status == "Resolved" || t.Status == "Closed") })
            .OrderByDescending(x => x.resolved).ToList();
        var feedback = _catalog.GetFeedbackStats();
        var activity = _notifications.Recent(15);

        return Ok(new
        {
            openTickets = open,
            inProgress,
            resolvedToday,
            avgResponseMinutes = avgResponseMin,
            avgResolutionHours = avgResolveHrs,
            byStatus,
            byAgent,
            feedback,
            activity
        });
    }
}
