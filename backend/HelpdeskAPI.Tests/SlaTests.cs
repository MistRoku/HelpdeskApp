using HelpdeskAPI.Models;
using HelpdeskAPI.Services;

namespace HelpdeskAPI.Tests;

public class SlaTests
{
    [Theory]
    [InlineData("Urgent", 15, 240)]
    [InlineData("High", 60, 1440)]
    [InlineData("Medium", 240, 4320)]
    [InlineData("Low", 1440, 10080)]
    public void Policy_Deadlines_Match_Priority(string priority, int responseMin, int resolveMin)
    {
        var policy = SlaDefaults.ForPriority(priority);
        Assert.Equal(responseMin, policy.ResponseMinutes);
        Assert.Equal(resolveMin, policy.ResolutionMinutes);
    }

    [Fact]
    public void ApplyTo_Sets_Future_Due_Dates()
    {
        var ticket = new Ticket { Priority = "High" };
        var before = DateTime.UtcNow;
        SlaDefaults.ApplyTo(ticket);
        Assert.NotNull(ticket.ResponseDueAt);
        Assert.NotNull(ticket.ResolutionDueAt);
        Assert.True(ticket.ResponseDueAt > before);
        Assert.True(ticket.ResolutionDueAt > ticket.ResponseDueAt);
    }

    [Fact]
    public void Unknown_Priority_Falls_Back_To_Medium()
    {
        var policy = SlaDefaults.ForPriority("NotARealPriority");
        Assert.Equal("Medium", policy.Priority);
    }
}
