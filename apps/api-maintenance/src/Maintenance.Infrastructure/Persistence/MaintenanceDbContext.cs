using Maintenance.Domain.Maintenance;
using Maintenance.Domain.Users;
using Maintenance.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Persistence;

/// <summary>
/// The application's single database context and unit of work.
/// Entity sets are added by the slices that introduce each entity.
/// </summary>
public class MaintenanceDbContext(DbContextOptions<MaintenanceDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserToken> UserTokens => Set<UserToken>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MaintenanceDbContext).Assembly);
    }
}
