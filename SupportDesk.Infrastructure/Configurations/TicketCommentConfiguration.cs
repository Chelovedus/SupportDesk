using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain;

namespace SupportDesk.Infrastructure.Configurations;

public class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("ticket_comments");
        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.TicketId)
            .IsRequired()
            .HasColumnName("ticket_id");
        builder.Property(comment => comment.AuthorUserId)
            .IsRequired()
            .HasColumnName("author_user_id");
        builder.Property(comment => comment.CommentText)
            .IsRequired()
            .HasColumnName("comment_text");
        builder.Property(comment => comment.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");
        
        
    }
}