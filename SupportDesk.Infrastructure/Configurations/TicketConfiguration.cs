using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain;

namespace SupportDesk.Infrastructure.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");
        builder.HasKey(ticket => ticket.Id);
        
        builder.Property(ticket => ticket.Title)
            .IsRequired()
            .HasColumnName("title");
        builder.Property(ticket => ticket.Description)
            .IsRequired()
            .HasColumnName("description");
        builder.Property(ticket => ticket.Priority)
            .IsRequired()
            .HasColumnName("priority");
        builder.Property(ticket => ticket.Status)
            .IsRequired()
            .HasColumnName("status");
        builder.Property(ticket => ticket.CreatedByUserId)
            .IsRequired()
            .HasColumnName("created_by_user_id");
        builder.Property(ticket => ticket.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");
        builder.Property(ticket => ticket.UpdatedAt)
            .IsRequired()
            .HasColumnName("updated_at");
        builder.Property(ticket => ticket.AssignedAgentId)
            .HasColumnName("assigned_agent_id");
        builder.Property(ticket => ticket.ResolvedAt)
            .HasColumnName("resolved_at");
        builder.Property(ticket => ticket.ClosedAt)
            .HasColumnName("closed_at");

        builder.HasMany(ticket => ticket.History)
            .WithOne()
            .HasForeignKey(historyItem => historyItem.TicketId);
        builder.HasMany(ticket => ticket.Comments)
            .WithOne()
            .HasForeignKey(comment => comment.TicketId);

        builder.HasIndex(ticket => ticket.Status)
            .HasDatabaseName("IX_tickets_status");
        builder.HasIndex(ticket => ticket.Priority)
            .HasDatabaseName("IX_tickets_priority");
        builder.HasIndex(ticket => ticket.CreatedAt)
            .HasDatabaseName("IX_tickets_created_at");
        builder.HasIndex(ticket => ticket.CreatedByUserId)
            .HasDatabaseName("IX_tickets_created_by_user_id");
        builder.HasIndex(ticket => ticket.AssignedAgentId)
            .HasDatabaseName("IX_tickets_assigned_agent_id");
        
    }
}