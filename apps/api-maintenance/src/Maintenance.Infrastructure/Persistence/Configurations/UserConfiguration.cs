using Maintenance.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="User"/> to the USERS table of the design.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasColumnName("id");
        builder.Property(user => user.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(user => user.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        builder.Property(user => user.EmailVerified).HasColumnName("email_verified").IsRequired().HasDefaultValue(false);
        builder.Property(user => user.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.HasIndex(user => user.Email).IsUnique().HasDatabaseName("ux_users_email");
    }
}
