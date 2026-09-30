using HelpdeskAPI.Data;

namespace HelpdeskAPI.Services;

/// <summary>
/// SLA sweeper. Runs every 5 minutes: flags response and resolution
/// breaches, notifies the assignee and the lead queue. Near-due tickets
/// (under 30 min to response target) get a warning notification.
/// </summary>
public class SlaSweeper : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<SlaSweeper> _logger;

    public SlaSweeper(IServiceProvider provider, ILogger<SlaSweeper> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _provider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<ITicketRepository>();
                var notifications = scope.ServiceProvider.GetRequiredService<NotificationService>();
                var now = DateTime.UtcNow;
                var all = await repo.GetAllAsync();
                foreach (var t in all.Where(t => t.Status != "Closed" && t.Status != "Resolved"))
                {
                    if ((t.ResponseDueAt != null && now > t.ResponseDueAt) ||
                        (t.ResolutionDueAt != null && now > t.ResolutionDueAt))
                    {
                        if (!t.SlaBreached)
                        {
                            // Mutate the tracked instance directly: UpdateAsync would
                            // recompute due dates from now and clear the flag.
                            t.SlaBreached = true;
                            notifications.Notify(t.AssignedTo ?? "agent1", "SLA breached on " + t.TicketNumber, t.Title, t.Id);
                            notifications.Notify("team-lead", "Escalation: " + t.TicketNumber + " breached SLA", t.Title, t.Id);
                            notifications.Audit(t.Id, "sla-sweeper", "Flagged SLA breach");
                        }
                    }
                    else if (t.ResponseDueAt != null && (t.ResponseDueAt.Value - now).TotalMinutes is var mins && mins is > 0 and < 30)
                    {
                        notifications.Notify(t.AssignedTo ?? "agent1", "SLA warning on " + t.TicketNumber, "Response due in " + (int)mins + " min.", t.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SLA sweep failed, will retry.");
            }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
