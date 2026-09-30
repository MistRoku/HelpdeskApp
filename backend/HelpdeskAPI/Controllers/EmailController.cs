using Microsoft.AspNetCore.Mvc;
using HelpdeskAPI.Data;
using HelpdeskAPI.Models;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Controllers;

/// <summary>
/// Inbound email webhook. Parses subject for ticket reference, creates or appends.
/// In production this is called by SendGrid or Graph with a shared secret header.
/// </summary>
[Route("api/email")]
[ApiController]
public class EmailController : ControllerBase
{
    private readonly ITicketRepository _tickets;

    public EmailController(ITicketRepository tickets) => _tickets = tickets;

    public record InboundEmail(string From, string Subject, string Body);

    [HttpPost("inbound")]
    public async Task<ActionResult> Inbound([FromBody] InboundEmail email, [FromHeader(Name = "X-Email-Secret")] string? secret)
    {
        var expected = Environment.GetEnvironmentVariable("EMAIL_WEBHOOK_SECRET") ?? "dev-secret-change-me";
        if (secret != expected) return Unauthorized();

        var subject = SanitizerService.Sanitize(email.Subject, 200);
        var body = SanitizerService.Sanitize(email.Body, 10000);
        var from = SanitizerService.Sanitize(email.From, 200);

        // Reply detection: subject contains [TKT-123] or #123
        var match = System.Text.RegularExpressions.Regex.Match(subject, @"#?(\d{1,6})");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var ticketId))
        {
            var existing = await _tickets.GetByIdAsync(ticketId);
            if (existing != null)
            {
                existing.Description += " [Email reply from " + from + "]: " + body;
                existing.UpdatedAt = DateTime.UtcNow;
                await _tickets.UpdateAsync(existing);
                return Ok(new { ticketId, action = "appended" });
            }
        }

        var created = await _tickets.CreateAsync(new Ticket
        {
            Title = string.IsNullOrWhiteSpace(subject) ? "Email request" : subject,
            Description = body,
            UserName = from,
            Channel = "Email",
            Status = "Open",
            Priority = "Medium",
        });
        return Ok(new { ticketId = created.Id, action = "created" });
    }

    [HttpGet("log")]
    public ActionResult Log() => Ok(new[]
    {
        new { id = 1, direction = "Inbound", from = "customer@example.com", subject = "Printer offline", receivedAt = DateTime.UtcNow.AddHours(-3) },
        new { id = 2, direction = "Outbound", to = "customer@example.com", subject = "Ticket #2 received", receivedAt = DateTime.UtcNow.AddHours(-2) },
    });
}
