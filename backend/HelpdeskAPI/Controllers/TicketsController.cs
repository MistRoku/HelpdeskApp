using Microsoft.AspNetCore.Mvc;
using HelpdeskAPI.Data;
using HelpdeskAPI.Models;

[Route("api/[controller]")]
[ApiController]
public class TicketsController : ControllerBase
{
    private readonly ITicketRepository _repository;

    public TicketsController(ITicketRepository repository) => _repository = repository;

    // GET: api/tickets
    [HttpGet]
    public async Task<ActionResult<List<Ticket>>> GetAll() => Ok(await _repository.GetAllAsync());

    // GET: api/tickets/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Ticket>> Get(int id)
    {
        var ticket = await _repository.GetByIdAsync(id);
        return ticket == null ? NotFound() : Ok(ticket);
    }

    // POST: api/tickets
    [HttpPost]
    public async Task<ActionResult<Ticket>> Post(Ticket ticket)
    {
        var created = await _repository.CreateAsync(ticket);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    // PUT: api/tickets/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(int id, Ticket ticket)
    {
        if (id != ticket.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(ticket);
        return updated ? NoContent() : NotFound();
    }

    // DELETE: api/tickets/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // GET: api/tickets/user/john.doe
    [HttpGet("user/{username}")]
    public async Task<ActionResult<List<Ticket>>> GetUserTickets(string username) =>
        Ok(await _repository.GetUserTicketsAsync(username));
}