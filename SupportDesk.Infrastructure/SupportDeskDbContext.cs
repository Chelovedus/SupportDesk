using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain;
using SupportDesk.Domain.Users;
using SupportDesk.Infrastructure.Configurations;

namespace SupportDesk.Infrastructure;

public class SupportDeskDbContext : DbContext
{
    public SupportDeskDbContext(DbContextOptions<SupportDeskDbContext> options)
        : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketHistoryItem> TicketHistoryItems => Set<TicketHistoryItem>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // modelBuilder.ApplyConfiguration(new TicketConfiguration());
        // modelBuilder.ApplyConfiguration(new TicketCommentConfiguration());
        // modelBuilder.ApplyConfiguration(new TicketHistoryConfiguration());
        // WIP
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SupportDeskDbContext).Assembly);
    }
}