using FluentAssertions;
using Maintenance.Domain.Users;

namespace Maintenance.UnitTests.Users;

public class UserTokenTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    private static UserToken Issue() => UserToken.Issue(Guid.NewGuid(), TokenPurpose.EmailVerification, "hash", Now, Lifetime);

    [Fact]
    public void Issue_SetsExpiryFromLifetime_AndStartsUnused()
    {
        var token = Issue();

        token.ExpiresAt.Should().Be(Now + Lifetime);
        token.UsedAt.Should().BeNull();
        token.IsUsable(TokenPurpose.EmailVerification, Now).Should().BeTrue();
    }

    [Fact]
    public void IsUsable_AfterExpiry_IsFalse() =>
        Issue().IsUsable(TokenPurpose.EmailVerification, Now + Lifetime).Should().BeFalse();

    [Fact]
    public void IsUsable_ForAnotherPurpose_IsFalse() =>
        Issue().IsUsable(TokenPurpose.PasswordReset, Now).Should().BeFalse();

    [Fact]
    public void MarkUsed_ConsumesOnce_AndKeepsFirstTimestamp()
    {
        var token = Issue();

        token.MarkUsed(Now.AddMinutes(1));
        token.MarkUsed(Now.AddMinutes(5));

        token.UsedAt.Should().Be(Now.AddMinutes(1));
        token.IsUsable(TokenPurpose.EmailVerification, Now.AddMinutes(2)).Should().BeFalse();
    }
}
