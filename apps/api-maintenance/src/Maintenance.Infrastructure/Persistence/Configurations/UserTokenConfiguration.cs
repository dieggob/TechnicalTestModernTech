using Maintenance.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="UserToken"/> to the USER_TOKENS table of the design.</summary>
public sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("user_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).HasColumnName("id");
        builder.Property(token => token.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(token => token.Purpose).HasColumnName("purpose").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        builder.Property(token => token.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(token => token.UsedAt).HasColumnName("used_at");
        builder.Property(token => token.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(token => token.TokenHash).IsUnique().HasDatabaseName("ux_user_tokens_hash");
        builder.HasIndex(token => new { token.UserId, token.Purpose }).HasDatabaseName("ix_user_tokens_user_purpose");
    }
}
