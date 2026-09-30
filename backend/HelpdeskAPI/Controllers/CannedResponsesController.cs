using Microsoft.AspNetCore.Mvc;
using HelpdeskAPI.Models;
using HelpdeskAPI.Services;

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
        var me = RequestUserHelper.Current(HttpContext);
        if (!RequestUserHelper.IsAgentOrAdmin(me)) return Forbid();
        if (string.IsNullOrWhiteSpace(response.Title) || string.IsNullOrWhiteSpace(response.Content))
            return BadRequest("Title and content are required.");
        return Ok(_store.CreateCanned(response, me.Username));
    }

    [HttpPost("{id}/use")]
    public ActionResult TrackUse(int id)
    {
        _store.TrackCannedUse(id);
        return Ok();
    }
}
