using Microsoft.AspNetCore.SignalR;

namespace HelpdeskAPI.Hubs;

/// <summary>
/// Live ticket updates. Clients join group per ticket id for scoped messages.
/// </summary>
public class TicketHub : Hub
{
    public Task JoinTicket(int ticketId) => Groups.AddToGroupAsync(Context.ConnectionId, "ticket-" + ticketId);
    public Task LeaveTicket(int ticketId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, "ticket-" + ticketId);
}
