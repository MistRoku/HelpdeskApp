using HelpdeskAPI.Models;

namespace HelpdeskAPI.Data;

public class TicketRepository : ITicketRepository
{
	private static List<Ticket> _tickets = new()
	{
		new() { Id = 1, Title = "Login Issue", Description = "Can't login", UserName = "john.doe", Status = "Open" },
		new() { Id = 2, Title = "Printer not working", Description = "HP printer offline", UserName = "jane.smith", Status = "InProgress", AssignedTo = "admin" }
	};
	private static int _nextId = 3;

	public Task<List<Ticket>> GetAllAsync() => Task.FromResult(_tickets);
	public Task<Ticket?> GetByIdAsync(int id) => Task.FromResult(_tickets.FirstOrDefault(t => t.Id == id));

	public Task<Ticket> CreateAsync(Ticket ticket)
	{
		ticket.Id = _nextId++;
		_tickets.Add(ticket);
		return Task.FromResult(ticket);
	}

	public Task<bool> UpdateAsync(Ticket ticket)
	{
		var existing = _tickets.FirstOrDefault(t => t.Id == ticket.Id);
		if (existing == null) return Task.FromResult(false);

		existing.Title = ticket.Title;
		existing.Description = ticket.Description;
		existing.Status = ticket.Status;
		existing.Priority = ticket.Priority;
		existing.AssignedTo = ticket.AssignedTo;
		existing.UpdatedAt = DateTime.UtcNow;

		return Task.FromResult(true);
	}

	public Task<bool> DeleteAsync(int id)
	{
		var ticket = _tickets.FirstOrDefault(t => t.Id == id);
		if (ticket == null) return Task.FromResult(false);
		_tickets.Remove(ticket);
		return Task.FromResult(true);
	}

	public Task<List<Ticket>> GetUserTicketsAsync(string username) =>
		Task.FromResult(_tickets.Where(t => t.UserName == username).ToList());

	public Task<List<Ticket>> GetAdminTicketsAsync() => Task.FromResult(_tickets);
}/

