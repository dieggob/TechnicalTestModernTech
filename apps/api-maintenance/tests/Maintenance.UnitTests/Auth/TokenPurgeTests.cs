using FluentAssertions;
using Maintenance.Application.Auth;
using Maintenance.Application.Time;
using Maintenance.Domain.Users;
using Maintenance.Infrastructure.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Maintenance.UnitTests.Auth;

public class TokenPurgeTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task StartAsync_DeletesTokensCreatedBeforeTheRetentionWindow()
    {
        var tokens = Substitute.For<IUserTokenRepository>();
        tokens.DeleteCreatedBeforeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(3);
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);
        var services = new ServiceCollection().AddScoped(_ => tokens).BuildServiceProvider();
        var purge = new TokenPurgeOnStartup(services.GetRequiredService<IServiceScopeFactory>(), clock,
            Options.Create(new TokenOptions { PurgeAfter = TimeSpan.FromDays(30) }), NullLogger<TokenPurgeOnStartup>.Instance);

        await purge.StartAsync(CancellationToken.None);

        await tokens.Received(1).DeleteCreatedBeforeAsync(Now.AddDays(-30), Arg.Any<CancellationToken>());
    }
}
