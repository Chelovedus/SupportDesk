using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain;

namespace SupportDesk.Infrastructure.Configurations;

public class TicketHistoryConfiguration : IEntityTypeConfiguration<TicketHistoryItem>
{
    public void Configure(EntityTypeBuilder<TicketHistoryItem> builder)
    {
        builder.ToTable("ticket_history");
        builder.HasKey(historyItem => historyItem.Id);
        
        builder.Property(historyItem => historyItem.TicketId)
            .IsRequired()
            .HasColumnName("ticket_id");
        builder.Property(historyItem => historyItem.Details)
            .IsRequired()
            .HasColumnName("details");
        builder.Property(historyItem => historyItem.ActorUserId)
            .IsRequired()
            .HasColumnName("actor_user_id");
        builder.Property(historyItem => historyItem.Action)
            .IsRequired()
            .HasColumnName("action");
        builder.Property(historyItem => historyItem.OldStatus)
            .IsRequired()
            .HasColumnName("old_status");
        builder.Property(historyItem => historyItem.NewStatus)
            .IsRequired()
            .HasColumnName("new_status");
        builder.Property(historyItem => historyItem.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");
        
    }
}