using HelpdeskAPI.Models;

namespace HelpdeskAPI.Services;

public static class SlaDefaults
{
    public static readonly List<SlaPolicy> Policies = new()
    {
        new() { Id = 1, Name = "Urgent", Priority = "Urgent", ResponseMinutes = 15, ResolutionMinutes = 240, IsActive = true },
        new() { Id = 2, Name = "High", Priority = "High", ResponseMinutes = 60, ResolutionMinutes = 1440, IsActive = true },
        new() { Id = 3, Name = "Medium", Priority = "Medium", ResponseMinutes = 240, ResolutionMinutes = 4320, IsActive = true },
        new() { Id = 4, Name = "Low", Priority = "Low", ResponseMinutes = 1440, ResolutionMinutes = 10080, IsActive = true },
    };

    public static SlaPolicy ForPriority(string priority) =>
        Policies.FirstOrDefault(p => p.Priority.Equals(priority, StringComparison.OrdinalIgnoreCase)) ?? Policies[2];

    public static void ApplyTo(Ticket ticket)
    {
        var policy = ForPriority(ticket.Priority);
        var now = DateTime.UtcNow;
        ticket.ResponseDueAt = now.AddMinutes(policy.ResponseMinutes);
        ticket.ResolutionDueAt = now.AddMinutes(policy.ResolutionMinutes);
    }
}
