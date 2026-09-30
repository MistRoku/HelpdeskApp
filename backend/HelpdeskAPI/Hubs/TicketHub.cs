using Microsoft.AspNetCore.SignalR;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Hubs;

/// <summary>
/// Live ticket updates. Ticket viewers join ticket-{id}; agents join
/// agents and agent-{username} for assignment and queue events.
/// Controllers broadcast create, status, reply and assign events here.
/// Client reconnects with backoff (see frontend hooks/useSignalR).
/// </summary>
public class TicketHub : Hub
{
    public Task JoinTicket(int ticketId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, TicketGroups.Ticket(ticketId));

    public Task LeaveTicket(int ticketId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, TicketGroups.Ticket(ticketId));

    public Task JoinAgents() =>
        Groups.AddToGroupAsync(Context.ConnectionId, TicketGroups.Agents);

    public Task JoinAgent(string username)
    {
        var me = Context.User?.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(me) && !me.Equals(username, StringComparison.OrdinalIgnoreCase))
            throw new HubException("Cannot join another agent group.");
        var clean = SanitizerService.Sanitize(username, 100);
        return Groups.AddToGroupAsync(Context.ConnectionId, TicketGroups.Agent(clean));
    }
}

public static class TicketGroups
{
    public static string Ticket(int id) => "ticket-" + id;
    public const string Agents = "agents";
    public static string Agent(string username) => "agent-" + username.ToLowerInvariant();
}
