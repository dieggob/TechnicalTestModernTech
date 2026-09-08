using Maintenance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Maintenance.IntegrationTests;

/// <summary>
/// Boots the real API pipeline in-process against a private SQLite in-memory database and
/// adds the test-only endpoints. The connection stays open for the factory's lifetime; an
/// in-memory SQLite database disappears when its last connection closes.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MaintenanceDbContext>>();
            services.AddDbContext<MaintenanceDbContext>(options => options.UseSqlite(_connection));
            services.AddTransient<IStartupFilter, TestEndpoints.StartupFilter>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

internal static class ServiceCollectionExtensions
{
    public static void RemoveAll<TService>(this IServiceCollection services)
    {
        foreach (var descriptor in services.Where(d => d.ServiceType == typeof(TService)).ToList())
        {
            services.Remove(descriptor);
        }
    }
}
