using Microsoft.AspNetCore.Mvc;
using HelpdeskAPI.Models;

namespace HelpdeskAPI.Controllers;

[Route("api/feedback")]
[ApiController]
public class FeedbackController : ControllerBase
{
    private readonly Data.CatalogStore _store;
    public FeedbackController(Data.CatalogStore store) => _store = store;

    [HttpPost("submit")]
    [ValidateAntiForgeryToken]
    public ActionResult Submit([FromBody] FeedbackEntry entry)
    {
        if (entry.Type != "CSAT" && entry.Type != "NPS") return BadRequest("Type must be CSAT or NPS.");
        if (entry.Type == "CSAT" && (entry.Score < 1 || entry.Score > 5)) return BadRequest("CSAT score must be 1 to 5.");
        if (entry.Type == "NPS" && (entry.Score < 0 || entry.Score > 10)) return BadRequest("NPS score must be 0 to 10.");
        return Ok(_store.AddFeedback(entry));
    }

    [HttpGet("stats")]
    public ActionResult Stats() => Ok(_store.GetFeedbackStats());

    [HttpGet]
    public ActionResult All() => Ok(_store.GetFeedback());
}
