using Maintenance.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Persistence.Repositories;

public sealed class VehicleRepository(MaintenanceDbContext context) : IVehicleRepository
{
    public Task<Vehicle?> FindByIdAndUserIdAsync(Guid vehicleId, Guid userId, CancellationToken cancellationToken) =>
        context.Vehicles.SingleOrDefaultAsync(vehicle => vehicle.Id == vehicleId && vehicle.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> FindAllByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        await context.Vehicles
            .Where(vehicle => vehicle.UserId == userId)
            .OrderBy(vehicle => vehicle.Make).ThenBy(vehicle => vehicle.Model).ThenBy(vehicle => vehicle.Year)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsVinAsync(Guid userId, string vin, Guid? excludingVehicleId, CancellationToken cancellationToken) =>
        context.Vehicles.AnyAsync(
            vehicle => vehicle.UserId == userId && vehicle.Vin == vin && (excludingVehicleId == null || vehicle.Id != excludingVehicleId),
            cancellationToken);

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken) =>
        await context.Vehicles.AddAsync(vehicle, cancellationToken);

    public void Remove(Vehicle vehicle) => context.Vehicles.Remove(vehicle);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
