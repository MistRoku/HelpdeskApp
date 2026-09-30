using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using HelpdeskAPI.Data;
using HelpdeskAPI.Hubs;
using HelpdeskAPI.Models;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TicketsController : ControllerBase
{
    private readonly ITicketRepository _repository;
    private readonly AiClassificationService _ai;
    private readonly NotificationService _notifications;
    private readonly IHubContext<TicketHub> _hub;

    public TicketsController(ITicketRepository repository, AiClassificationService ai, NotificationService notifications, IHubContext<TicketHub> hub)
    {
        _repository = repository;
        _ai = ai;
        _notifications = notifications;
        _hub = hub;
    }

    private RequestUser Me() => RequestUserHelper.Current(HttpContext);

    // GET /api/tickets?q=&status=&priority=&category=&assignee=&from=&to=
    [HttpGet]
    public async Task<ActionResult<List<Ticket>>> GetAll(
        [FromQuery] string? q, [FromQuery] string? status, [FromQuery] string? priority,
        [FromQuery] string? category, [FromQuery] string? assignee, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var me = Me();
        List<Ticket> list;
        if (q != null || status != null || priority != null || category != null || assignee != null || from != null || to != null)
            list = await _repository.SearchAsync(q, status, priority, category, assignee, from, to);
        else
            list = await _repository.GetAllAsync();

        // Customers only see their own tickets
        if (me.Role == "User" && me.Username != "anonymous")
            list = list.Where(t => t.UserName == me.Username).ToList();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Ticket>> Get(int id)
    {
        var me = Me();
        var ticket = await _repository.GetByIdAsync(id);
        if (ticket == null) return NotFound();
        if (me.Role == "User" && ticket.UserName != me.Username) return Forbid();
        return Ok(ticket);
    }

    [HttpGet("number/{number}")]
    public async Task<ActionResult<Ticket>> GetByNumber(string number)
    {
        var ticket = await _repository.GetByNumberAsync(number);
        return ticket == null ? NotFound() : Ok(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Ticket>> Post(Ticket ticket)
    {
        var me = Me();
        if (string.IsNullOrWhiteSpace(ticket.Title) || string.IsNullOrWhiteSpace(ticket.Description))
            return BadRequest("Title and description are required.");
        if (me.Username != "anonymous") ticket.UserName = me.Username;
        if (string.IsNullOrWhiteSpace(ticket.UserName)) ticket.UserName = "web-user";

        var caller = me.Username;
        if (string.IsNullOrWhiteSpace(ticket.Category) || ticket.Category == "General")
        {
            if (_ai.TryCheckCap(caller, out _))
            {
                var result = _ai.Classify(ticket.Title, ticket.Description, caller);
                if (!result.Blocked)
                {
                    ticket.Category = result.Category;
                    ticket.AiCategory = result.Category;
                    ticket.AiPriority = result.Priority;
                    ticket.AiConfidence = result.Confidence;
                    if (string.IsNullOrWhiteSpace(ticket.Priority) || ticket.Priority == "Medium")
                        ticket.Priority = result.Priority;
                }
            }
        }

        var created = await _repository.CreateAsync(ticket);
        _notifications.Audit(created.Id, caller, "Created ticket " + created.TicketNumber);
        _notifications.Broadcast("New ticket " + created.TicketNumber, created.Title, created.Id);
        await _hub.Clients.All.SendAsync("ticketCreated", created.Id, created.TicketNumber);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Put(int id, Ticket ticket)
    {
        var me = Me();
        if (id != ticket.Id) return BadRequest();
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound();
        if (me.Role == "User" && existing.UserName != me.Username) return Forbid();
        if (!RequestUserHelper.IsAgentOrAdmin(me))
        {
            // Customers may edit title and description of own open tickets only
            if (existing.Status == "Closed" || existing.Status == "Resolved") return Forbid();
        }
        var updated = await _repository.UpdateAsync(ticket);
        if (updated)
        {
            _notifications.Audit(id, me.Username, "Updated ticket");
            await _hub.Clients.Group("ticket-" + id).SendAsync("ticketUpdated", id);
        }
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!RequestUserHelper.IsAdmin(Me())) return Forbid();
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPut("{id}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusRequest req)
    {
        var me = Me();
        var ticket = await _repository.GetByIdAsync(id);
        if (ticket == null) return NotFound();

        if (!WorkflowService.CanTransition(ticket.Status, req.Status))
            return BadRequest("Transition from " + ticket.Status + " to " + req.Status + " is not allowed.");

        if (req.Status == "Open" && ticket.Status == "Closed")
        {
            // Reopen within 7 days
            if (!WorkflowService.CanReopen(ticket.ClosedAt))
                return BadRequest("Closed tickets can only be reopened within 7 days.");
            if (me.Role == "User" && ticket.UserName != me.Username) return Forbid();
        }
        else if (me.Role == "User")
        {
            // Customers may only close or reopen own tickets
            if (!(req.Status == "Closed" && ticket.UserName == me.Username)) return Forbid();
        }

        ticket.Status = req.Status;
        await _repository.UpdateAsync(ticket);
        _notifications.Audit(id, me.Username, "Status to " + req.Status);
        _notifications.Notify(ticket.UserName, "Ticket " + ticket.TicketNumber + " is " + req.Status, "Status changed by " + me.Username, id);
        await _hub.Clients.Group("ticket-" + id).SendAsync("ticketUpdated", id);
        return NoContent();
    }

    [HttpPut("{id}/assign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignRequest req)
    {
        var me = Me();
        if (!RequestUserHelper.IsAgentOrAdmin(me)) return Forbid();
        var ticket = await _repository.GetByIdAsync(id);
        if (ticket == null) return NotFound();
        ticket.AssignedTo = SanitizerService.Sanitize(req.Assignee, 100);
        if (ticket.Status == "New") ticket.Status = "Open";
        await _repository.UpdateAsync(ticket);
        _notifications.Notify(req.Assignee, "Assigned ticket " + ticket.TicketNumber, ticket.Title, id);
        _notifications.Audit(id, me.Username, "Assigned to " + req.Assignee);
        await _hub.Clients.All.SendAsync("ticketAssigned", id, req.Assignee);
        return NoContent();
    }

    [HttpPost("{id}/reopen")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(int id)
    {
        var me = Me();
        var ticket = await _repository.GetByIdAsync(id);
        if (ticket == null) return NotFound();
        if (ticket.Status != "Closed" && ticket.Status != "Resolved") return BadRequest("Only resolved or closed tickets can be reopened.");
        if (me.Role == "User" && ticket.UserName != me.Username) return Forbid();
        if (!WorkflowService.CanReopen(ticket.ClosedAt ?? ticket.ResolvedAt))
            return BadRequest("Tickets can only be reopened within 7 days of closure.");
        ticket.Status = "Open";
        ticket.ClosedAt = null;
        await _repository.UpdateAsync(ticket);
        _notifications.Audit(id, me.Username, "Reopened ticket");
        return NoContent();
    }

    // Replies: public visible to customer, internal agents only
    [HttpGet("{id}/replies")]
    public async Task<ActionResult> Replies(int id)
    {
        var me = Me();
        var ticket = await _repository.GetByIdAsync(id);
        if (ticket == null) return NotFound();
        if (me.Role == "User" && ticket.UserName != me.Username) return Forbid();
        var includeInternal = RequestUserHelper.IsAgentOrAdmin(me);
        return Ok(await _repository.GetRepliesAsync(id, includeInternal, me.Username, me.Role));
    }

    [HttpPost("{id}/replies")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> AddReply(int id, [FromBody] ReplyRequest req)
    {
        var me = Me();
        var ticket = await _repository.GetByIdAsync(id);
        if (ticket == null) return NotFound();
        if (me.Role == "User" && ticket.UserName != me.Username) return Forbid();
        if (string.IsNullOrWhiteSpace(req.Body)) return BadRequest("Reply body is required.");
        if (req.IsInternal && !RequestUserHelper.IsAgentOrAdmin(me)) return Forbid();

        var reply = await _repository.AddReplyAsync(new TicketReply
        {
            TicketId = id,
            Author = me.Username == "anonymous" ? "web-user" : me.Username,
            Body = req.Body,
            IsInternal = req.IsInternal
        });
        var other = ticket.UserName == reply.Author ? (ticket.AssignedTo ?? "agent1") : ticket.UserName;
        _notifications.Notify(other, "New reply on " + ticket.TicketNumber, req.Body.Length > 120 ? req.Body.Substring(0, 120) : req.Body, id);
        await _hub.Clients.Group("ticket-" + id).SendAsync("replyAdded", id);
        return Ok(reply);
    }

    // Attachments: 10 MB max, allowlist enforced
    [HttpGet("{id}/attachments")]
    public async Task<ActionResult> Attachments(int id) => Ok(await _repository.GetAttachmentsAsync(id));

    [HttpPost("{id}/attachments")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult> Upload(int id, IFormFile file)
    {
        var me = Me();
        var ticket = await _repository.GetByIdAsync(id);
        if (ticket == null) return NotFound();
        if (file == null || file.Length == 0) return BadRequest("File is required.");
        if (file.Length > TicketRepository.MaxFileBytes) return BadRequest("File exceeds 10 MB limit.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!TicketRepository.AllowedExtensions.Contains(ext)) return BadRequest("File type not allowed. Use images, PDF, text, or office documents.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var saved = await _repository.AddAttachmentAsync(new TicketAttachment
        {
            TicketId = id,
            FileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            UploadedBy = me.Username,
            Content = ms.ToArray()
        });
        return Ok(new { saved.Id, saved.FileName, saved.SizeBytes });
    }

    [HttpGet("attachments/{attachmentId}/download")]
    public async Task<ActionResult> Download(int attachmentId)
    {
        var me = Me();
        var file = await _repository.GetAttachmentAsync(attachmentId);
        if (file == null) return NotFound();
        var ticket = await _repository.GetByIdAsync(file.TicketId);
        if (ticket != null && me.Role == "User" && ticket.UserName != me.Username) return Forbid();
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("user/{username}")]
    public async Task<ActionResult<List<Ticket>>> GetUserTickets(string username) =>
        Ok(await _repository.GetUserTicketsAsync(SanitizerService.Sanitize(username, 100)));

    [HttpGet("breached")]
    public async Task<ActionResult<List<Ticket>>> GetBreached() => Ok(await _repository.GetBreachedAsync());

    [HttpGet("export")]
    public async Task<ActionResult> Export([FromQuery] string format = "csv")
    {
        if (!RequestUserHelper.IsAdmin(Me())) return Forbid();
        var all = await _repository.GetAllAsync();
        var csv = "TicketNumber,Title,Status,Priority,Category,Assignee,CreatedAt\n" +
            string.Join("\n", all.Select(t => $"\"{t.TicketNumber}\",\"{t.Title.Replace("\"", "\"\"")}\",{t.Status},{t.Priority},{t.Category},{t.AssignedTo},{t.CreatedAt:O}"));
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "tickets.csv");
    }

    public record StatusRequest(string Status);
    public record AssignRequest(string Assignee);
    public record ReplyRequest(string Body, bool IsInternal);
}
