using Maintenance.Application.Auth;
using Maintenance.Application.Time;
using Maintenance.Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maintenance.Infrastructure.Auth;

/// <summary>
/// Deletes verification and reset tokens older than the retention window once, at start-up.
/// Every token is used or expired long before then (links live 30 minutes), so nothing usable
/// is ever removed. No scheduler: the design assumes a start-up sweep is enough locally.
/// </summary>
public sealed class TokenPurgeOnStartup(
    IServiceScopeFactory scopes,
    IClock clock,
    IOptions<TokenOptions> options,
    ILogger<TokenPurgeOnStartup> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IUserTokenRepository>();

        var cutoff = clock.UtcNow - options.Value.PurgeAfter;
        var deleted = await tokens.DeleteCreatedBeforeAsync(cutoff, cancellationToken);
        logger.LogInformation("Purged {Count} tokens created before {Cutoff}", deleted, cutoff);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
