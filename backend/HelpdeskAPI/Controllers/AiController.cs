using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Controllers;

[Route("api/ai")]
[ApiController]
public class AiController : ControllerBase
{
    private readonly AiClassificationService _ai;
    private readonly Data.CatalogStore _store;

    public AiController(AiClassificationService ai, Data.CatalogStore store)
    {
        _ai = ai;
        _store = store;
    }

    public record ClassifyRequest(string Subject, string Description);
    public record SuggestRequest(string Text);

    [HttpPost("classify")]
    public ActionResult Classify([FromBody] ClassifyRequest req)
    {
        var caller = User?.Identity?.Name ?? Request.Headers["X-User"].ToString();
        if (string.IsNullOrWhiteSpace(caller)) caller = "anonymous";

        if (!_ai.TryCheckCap(caller, out var remaining))
            return StatusCode(429, new { error = "AI daily limit reached. Try again tomorrow.", remaining = 0 });

        var cleanSubject = SanitizerService.Sanitize(req.Subject, 200);
        var cleanDescription = SanitizerService.Sanitize(req.Description, 5000);
        var result = _ai.Classify(cleanSubject, cleanDescription, caller);
        if (result.Blocked)
            return BadRequest(new { error = "Request refused. Instruction override attempts are blocked.", remaining });

        return Ok(new { category = result.Category, priority = result.Priority, confidence = result.Confidence, suggestedArticle = result.SuggestedArticle, remaining });
    }

    [HttpPost("suggest")]
    public ActionResult Suggest([FromBody] SuggestRequest req)
    {
        var clean = SanitizerService.Sanitize(req.Text, 2000);
        var articles = _store.SearchArticles(clean.Length > 60 ? clean.Substring(0, 60) : clean, null).Take(3).ToList();
        return Ok(articles.Select(a => new { a.Id, a.Title, a.Category }));
    }

    /// <summary>
    /// Classify then match KB articles in the predicted category so the
    /// ticket form can show suggestions before submit.
    /// </summary>
    [HttpPost("suggest-for-ticket")]
    public ActionResult SuggestForTicket([FromBody] ClassifyRequest req)
    {
        var caller = User?.Identity?.Name ?? Request.Headers["X-User"].ToString();
        if (string.IsNullOrWhiteSpace(caller)) caller = "anonymous";
        if (!_ai.TryCheckCap(caller, out var remaining))
            return StatusCode(429, new { error = "AI daily limit reached. Try again tomorrow.", remaining = 0 });

        var result = _ai.Classify(
            SanitizerService.Sanitize(req.Subject, 200),
            SanitizerService.Sanitize(req.Description, 5000), caller);
        if (result.Blocked)
            return BadRequest(new { error = "Request refused. Instruction override attempts are blocked.", remaining });

        var articles = _store.SearchArticles(req.Subject, result.Category).Take(3).ToList();
        return Ok(new
        {
            category = result.Category,
            priority = result.Priority,
            confidence = result.Confidence,
            remaining,
            articles = articles.Select(a => new { a.Id, a.Title, a.Category })
        });
    }
}
