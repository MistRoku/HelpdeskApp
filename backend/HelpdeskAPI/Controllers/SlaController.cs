using Microsoft.AspNetCore.Mvc;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Controllers;

[Route("api/sla")]
[ApiController]
public class SlaController : ControllerBase
{
    private readonly Data.CatalogStore _store;

    public SlaController(Data.CatalogStore store) => _store = store;

    [HttpGet("policies")]
    public ActionResult GetPolicies() => Ok(SlaDefaults.Policies);

    [HttpGet("breaches")]
    public ActionResult GetBreaches() => Ok(_store.GetBreaches());

    [HttpGet("tickets/{id}/status")]
    public async Task<ActionResult> GetTicketSla(int id, [FromServices] Data.ITicketRepository repo)
    {
        var ticket = await repo.GetByIdAsync(id);
        if (ticket == null) return NotFound();
        var now = DateTime.UtcNow;
        var responseRemaining = ticket.ResponseDueAt == null ? (int?)null : (int)(ticket.ResponseDueAt.Value - now).TotalMinutes;
        var resolutionRemaining = ticket.ResolutionDueAt == null ? (int?)null : (int)(ticket.ResolutionDueAt.Value - now).TotalMinutes;
        var state = "OnTrack";
        if (responseRemaining < 0 || resolutionRemaining < 0) state = "Breached";
        else if (responseRemaining < 30 || resolutionRemaining < 120) state = "AtRisk";
        return Ok(new { ticketId = id, responseDueAt = ticket.ResponseDueAt, resolutionDueAt = ticket.ResolutionDueAt, responseRemainingMinutes = responseRemaining, resolutionRemainingMinutes = resolutionRemaining, state });
    }
}
