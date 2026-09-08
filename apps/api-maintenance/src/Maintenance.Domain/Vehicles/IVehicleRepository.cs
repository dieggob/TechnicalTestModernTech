namespace Maintenance.Domain.Vehicles;

/// <summary>
/// The only way to load vehicles, always scoped by owner, so no service can reach another
/// user's vehicle even by accident (design decision).
/// </summary>
public interface IVehicleRepository
{
    Task<Vehicle?> FindByIdAndUserIdAsync(Guid vehicleId, Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Vehicle>> FindAllByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Whether the user already has a vehicle with this VIN, optionally ignoring one vehicle (for updates).</summary>
    Task<bool> ExistsVinAsync(Guid userId, string vin, Guid? excludingVehicleId, CancellationToken cancellationToken);

    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken);

    void Remove(Vehicle vehicle);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
