using HelpdeskAPI.Models;

namespace HelpdeskAPI.Data;

public interface ITicketRepository
{
    Task<List<Ticket>> GetAllAsync();
    Task<Ticket?> GetByIdAsync(int id);
    Task<Ticket> CreateAsync(Ticket ticket);
    Task<bool> UpdateAsync(Ticket ticket);
    Task<bool> DeleteAsync(int id);
    Task<List<Ticket>> GetUserTicketsAsync(string username);
    Task<List<Ticket>> GetAdminTicketsAsync();
}