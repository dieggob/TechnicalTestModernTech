using Maintenance.Application.Auth;
using Maintenance.Application.Observability;
using Maintenance.Application.Time;
using Maintenance.Domain.Users;
using Maintenance.Infrastructure.Auth;
using Maintenance.Infrastructure.Email;
using Maintenance.Infrastructure.Observability;
using Maintenance.Infrastructure.Persistence;
using Maintenance.Infrastructure.Persistence.Repositories;
using Maintenance.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Maintenance.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseHealthCheckName = "database";

    /// <summary>
    /// Registers persistence, repositories, security and email adapters, the clock, metrics,
    /// and the database health check. The API host calls this once; tests replace the database
    /// connection through <see cref="MaintenanceDbContext"/> options.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<MaintenanceDbContext>(options => options.UseSqlite(connectionString));
        services.AddHealthChecks().AddDbContextCheck<MaintenanceDbContext>(DatabaseHealthCheckName);

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<ITokenGenerator, RandomTokenGenerator>();
        services.AddSingleton<LoggingEmailSender>();
        services.AddSingleton<IEmailSender>(provider => provider.GetRequiredService<LoggingEmailSender>());
        services.AddSingleton<IRecordedEmails>(provider => provider.GetRequiredService<LoggingEmailSender>());
        services.AddSingleton<IMaintenanceMetrics, MaintenanceMetrics>();

        return services;
    }

    /// <summary>
    /// Applies pending migrations so the schema always matches the running code.
    /// </summary>
    public static void MigrateDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>().Database.Migrate();
    }
}
