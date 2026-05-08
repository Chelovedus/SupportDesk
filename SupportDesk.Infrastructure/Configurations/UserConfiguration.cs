using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportDesk.Domain.Users;

namespace SupportDesk.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        
        builder.Property(user => user.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");
        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(320)
            .HasColumnName("email");
        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnName("password_hash");
        builder.Property(user => user.DisplayName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("display_name");
        builder.Property(user => user.Role)
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion<string>()
            .HasColumnName("role");
        builder.Property(user => user.CreatedAt)
            .IsRequired()
            .HasColumnName("created_at");
        
        builder.HasIndex(user => user.Email).IsUnique();
    }
}