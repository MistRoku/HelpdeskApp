using Microsoft.AspNetCore.Mvc;
using HelpdeskAPI.Data;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Controllers;

[Route("api/knowledgebase")]
[ApiController]
public class KnowledgeBaseController : ControllerBase
{
    private readonly CatalogStore _store;

    public KnowledgeBaseController(CatalogStore store) => _store = store;

    [HttpGet("articles")]
    public ActionResult Search([FromQuery] string? q, [FromQuery] string? category) =>
        Ok(_store.SearchArticles(q, category));

    [HttpGet("articles/{id}")]
    public ActionResult Get(int id)
    {
        var article = _store.GetArticle(id);
        if (article == null) return NotFound();
        _store.RecordView(id);
        return Ok(article);
    }

    // Agent and Admin only. Role check kept explicit, no default admin route exposed.
    [HttpPost("articles")]
    [ValidateAntiForgeryToken]
    public ActionResult Create([FromBody] HelpdeskAPI.Models.Article article)
    {
        if (!IsAgentOrAdmin()) return Forbid();
        if (string.IsNullOrWhiteSpace(article.Title) || string.IsNullOrWhiteSpace(article.Content))
            return BadRequest("Title and content are required.");
        var author = User?.Identity?.Name ?? "agent";
        return Ok(_store.CreateArticle(article, author));
    }

    [HttpPut("articles/{id}")]
    [ValidateAntiForgeryToken]
    public ActionResult Update(int id, [FromBody] HelpdeskAPI.Models.Article article)
    {
        if (!IsAgentOrAdmin()) return Forbid();
        return _store.UpdateArticle(id, article) ? NoContent() : NotFound();
    }

    [HttpDelete("articles/{id}")]
    [ValidateAntiForgeryToken]
    public ActionResult Delete(int id)
    {
        if (!IsAdmin()) return Forbid();
        return _store.DeleteArticle(id) ? NoContent() : NotFound();
    }

    [HttpPost("articles/{id}/feedback")]
    public ActionResult Feedback(int id, [FromBody] FeedbackRequest req)
    {
        _store.RecordFeedback(id, req.Helpful);
        return Ok();
    }

    private RequestUser Me() => RequestUserHelper.Current(HttpContext);

    private bool IsAgentOrAdmin() => RequestUserHelper.IsAgentOrAdmin(Me());

    private bool IsAdmin() => RequestUserHelper.IsAdmin(Me());

    public record FeedbackRequest(bool Helpful);
}
