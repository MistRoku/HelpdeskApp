using HelpdeskAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpdeskAPI.Data;

/// <summary>
/// EF Core persistence. SQLite for local demo, SQL Server in production.
/// Active when a connection string is configured; otherwise the app runs
/// on the in-memory repository so the demo works with zero setup.
/// Run: dotnet ef migrations add Initial --project backend/HelpdeskAPI
/// </summary>
public class HelpdeskDbContext : DbContext
{
    public HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options) : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketReply> Replies => Set<TicketReply>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.TicketNumber).IsUnique();
            e.HasIndex(t => t.Status);
            e.HasIndex(t => t.AssignedTo);
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
        });
        modelBuilder.Entity<TicketReply>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.TicketId);
        });
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
        });
        base.OnModelCreating(modelBuilder);
    }
}
