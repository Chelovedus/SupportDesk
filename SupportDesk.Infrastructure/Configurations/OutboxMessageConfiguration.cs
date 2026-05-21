using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain;

namespace SupportDesk.Infrastructure.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id");
        builder.Property(message => message.Type)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("type");
        builder.Property(message => message.PayloadJson)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasColumnName("payload_json");
        builder.Property(message => message.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("status");
        builder.Property(message => message.RetryCount)
            .IsRequired()
            .HasColumnName("retry_count");
        builder.Property(message => message.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");
        builder.Property(message => message.ProcessedAt)
            .HasColumnName("processed_at");
        builder.Property(message => message.LastError)
            .HasMaxLength(2000)
            .HasColumnName("last_error");

        builder.HasIndex(message => message.Status)
            .HasDatabaseName("IX_outbox_messages_status");
        builder.HasIndex(message => message.CreatedAt)
            .HasDatabaseName("IX_outbox_messages_created_at");
    }
}