using Microsoft.AspNetCore.Mvc;
using HelpdeskAPI.Models;

namespace HelpdeskAPI.Controllers;

[Route("api/canned")]
[ApiController]
public class CannedResponsesController : ControllerBase
{
    private readonly Data.CatalogStore _store;
    public CannedResponsesController(Data.CatalogStore store) => _store = store;

    [HttpGet]
    public ActionResult List() => Ok(_store.GetCanned());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create([FromBody] CannedResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.Title) || string.IsNullOrWhiteSpace(response.Content))
            return BadRequest("Title and content are required.");
        var owner = User?.Identity?.Name ?? "agent";
        return Ok(_store.CreateCanned(response, owner));
    }

    [HttpPost("{id}/use")]
    public ActionResult TrackUse(int id)
    {
        _store.TrackCannedUse(id);
        return Ok();
    }
}
