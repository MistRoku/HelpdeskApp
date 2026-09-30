using HelpdeskAPI.Models;

namespace HelpdeskAPI.Data;

public interface ITicketRepository
{
    Task<List<Ticket>> GetAllAsync();
    Task<Ticket?> GetByIdAsync(int id);
    Task<Ticket?> GetByNumberAsync(string ticketNumber);
    Task<Ticket> CreateAsync(Ticket ticket);
    Task<bool> UpdateAsync(Ticket ticket);
    Task<bool> DeleteAsync(int id);
    Task<List<Ticket>> GetUserTicketsAsync(string username);
    Task<List<Ticket>> GetAdminTicketsAsync();
    Task<List<Ticket>> SearchAsync(string? q, string? status, string? priority, string? category, string? assignee, DateTime? from, DateTime? to);

    Task<List<TicketReply>> GetRepliesAsync(int ticketId, bool includeInternal, string requester, string role);
    Task<TicketReply> AddReplyAsync(TicketReply reply);
    Task<List<TicketAttachment>> GetAttachmentsAsync(int ticketId);
    Task<TicketAttachment?> GetAttachmentAsync(int attachmentId);
    Task<TicketAttachment> AddAttachmentAsync(TicketAttachment attachment);
    Task AutoCloseIdleAsync();
    Task<List<Ticket>> GetBreachedAsync();
}
