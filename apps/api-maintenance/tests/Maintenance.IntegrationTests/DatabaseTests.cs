using FluentAssertions;
using Maintenance.Infrastructure;
using Maintenance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Maintenance.IntegrationTests;

public class DatabaseTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public DatabaseTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReportsDatabaseHealthy()
    {
        var healthChecks = _factory.Services.GetRequiredService<HealthCheckService>();

        var report = await healthChecks.CheckHealthAsync();

        report.Status.Should().Be(HealthStatus.Healthy);
        report.Entries.Should().ContainKey(DependencyInjection.DatabaseHealthCheckName)
            .WhoseValue.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Startup_AppliesMigrations()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>();

        var pending = await context.Database.GetPendingMigrationsAsync();
        var applied = await context.Database.GetAppliedMigrationsAsync();

        pending.Should().BeEmpty();
        applied.Should().NotBeEmpty();
    }
}
