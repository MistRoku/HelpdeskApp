using Microsoft.AspNetCore.Mvc;
using HelpdeskAPI.Data;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Controllers;

/// <summary>
/// Agent workload: unassigned queue, per-agent open counts, round-robin auto assign.
/// </summary>
[Route("api/workload")]
[ApiController]
public class WorkloadController : ControllerBase
{
    private readonly ITicketRepository _tickets;
    private readonly NotificationService _notifications;

    public WorkloadController(ITicketRepository tickets, NotificationService notifications)
    {
        _tickets = tickets;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<ActionResult> Get()
    {
        var me = RequestUserHelper.Current(HttpContext);
        if (!RequestUserHelper.IsAgentOrAdmin(me)) return Forbid();
        var all = await _tickets.GetAllAsync();
        var open = all.Where(t => t.Status != "Closed" && t.Status != "Resolved").ToList();
        return Ok(new
        {
            unassigned = open.Where(t => string.IsNullOrWhiteSpace(t.AssignedTo)).OrderBy(t => t.CreatedAt).Take(50),
            perAgent = open.Where(t => !string.IsNullOrWhiteSpace(t.AssignedTo))
                .GroupBy(t => t.AssignedTo!)
                .Select(g => new { agent = g.Key, open = g.Count(), urgent = g.Count(t => t.Priority == "Urgent") })
                .OrderBy(x => x.open)
        });
    }

    [HttpPost("auto-assign/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> AutoAssign(int id)
    {
        var me = RequestUserHelper.Current(HttpContext);
        if (!RequestUserHelper.IsAgentOrAdmin(me)) return Forbid();
        var ticket = await _tickets.GetByIdAsync(id);
        if (ticket == null) return NotFound();
        var all = await _tickets.GetAllAsync();
        var agents = all.Where(t => !string.IsNullOrWhiteSpace(t.AssignedTo))
            .GroupBy(t => t.AssignedTo!).ToDictionary(g => g.Key, g => g.Count(t => t.Status != "Closed"));
        var pool = new[] { "agent1", "agent2", "agent3" };
        var pick = pool.OrderBy(a => agents.TryGetValue(a, out var n) ? n : 0).First();
        ticket.AssignedTo = pick;
        if (ticket.Status == "New") ticket.Status = "Open";
        await _tickets.UpdateAsync(ticket);
        _notifications.Notify(pick, "Auto-assigned " + ticket.TicketNumber, ticket.Title, id);
        return Ok(new { assignee = pick });
    }
}
