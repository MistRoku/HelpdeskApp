using HelpdeskAPI.Services;

namespace HelpdeskAPI.Tests;

public class WorkflowTests
{
    [Theory]
    [InlineData("New", "Open", true)]
    [InlineData("Open", "InProgress", true)]
    [InlineData("InProgress", "Resolved", true)]
    [InlineData("Resolved", "Closed", true)]
    [InlineData("New", "Resolved", false)]
    [InlineData("Closed", "Resolved", false)]
    [InlineData("Pending", "Closed", false)]
    public void Transitions_Enforced(string from, string to, bool expected)
    {
        Assert.Equal(expected, WorkflowService.CanTransition(from, to));
    }

    [Fact]
    public void Reopen_Allowed_Within_7_Days()
    {
        Assert.True(WorkflowService.CanReopen(DateTime.UtcNow.AddDays(-6)));
        Assert.False(WorkflowService.CanReopen(DateTime.UtcNow.AddDays(-8)));
        Assert.False(WorkflowService.CanReopen(null));
    }

    [Fact]
    public void AutoClose_After_14_Days_Idle()
    {
        Assert.True(WorkflowService.ShouldAutoClose("Open", DateTime.UtcNow.AddDays(-15)));
        Assert.False(WorkflowService.ShouldAutoClose("Open", DateTime.UtcNow.AddDays(-2)));
        Assert.False(WorkflowService.ShouldAutoClose("Closed", DateTime.UtcNow.AddDays(-30)));
    }

    [Fact]
    public void TicketNumber_Has_Expected_Format()
    {
        var number = WorkflowService.NextTicketNumber();
        Assert.Matches(@"^TKT-\d{8}-\d+$", number);
    }
}
