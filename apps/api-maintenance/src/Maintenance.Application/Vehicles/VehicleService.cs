using FluentValidation;
using Maintenance.Application.Auth;
using Maintenance.Application.Exceptions;
using Maintenance.Application.Observability;
using Maintenance.Application.Time;
using Maintenance.Application.Validation;
using Maintenance.Domain.Vehicles;

namespace Maintenance.Application.Vehicles;

/// <summary>
/// Vehicle use cases for the calling user. Every lookup goes through the owner-scoped repository.
/// </summary>
public sealed class VehicleService(
    IVehicleRepository vehicles,
    ICurrentUser currentUser,
    IClock clock,
    IValidator<VehicleInput> validator,
    IMaintenanceMetrics metrics)
{
    public const string DuplicateVinMessage = "A vehicle with this VIN is already registered.";

    public async Task<VehicleDto> CreateAsync(VehicleInput input, CancellationToken cancellationToken)
    {
        input = input.Trimmed();
        ValidationRunner.Validate(validator, input);
        var userId = currentUser.UserId;

        await EnsureVinIsFreeAsync(userId, input.Vin, excludingVehicleId: null, cancellationToken);

        var vehicle = Vehicle.Create(userId, input.ToDetails(), clock.UtcNow);
        await vehicles.AddAsync(vehicle, cancellationToken);
        await vehicles.SaveChangesAsync(cancellationToken);
        metrics.VehicleCreated();

        return VehicleDto.From(vehicle);
    }

    public async Task<IReadOnlyList<VehicleDto>> ListAsync(CancellationToken cancellationToken)
    {
        var owned = await vehicles.FindAllByUserIdAsync(currentUser.UserId, cancellationToken);
        return owned.Select(VehicleDto.From).ToList();
    }

    public async Task<VehicleDto> GetAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        VehicleDto.From(await RequireOwnedAsync(vehicleId, cancellationToken));

    public async Task<VehicleDto> UpdateAsync(Guid vehicleId, VehicleInput input, CancellationToken cancellationToken)
    {
        input = input.Trimmed();
        ValidationRunner.Validate(validator, input);

        var vehicle = await RequireOwnedAsync(vehicleId, cancellationToken);
        await EnsureVinIsFreeAsync(vehicle.UserId, input.Vin, excludingVehicleId: vehicle.Id, cancellationToken);

        vehicle.Update(input.ToDetails(), clock.UtcNow);
        await vehicles.SaveChangesAsync(cancellationToken);
        return VehicleDto.From(vehicle);
    }

    /// <summary>Removes the vehicle; its maintenance records go with it by cascade (design decision).</summary>
    public async Task DeleteAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await RequireOwnedAsync(vehicleId, cancellationToken);
        vehicles.Remove(vehicle);
        await vehicles.SaveChangesAsync(cancellationToken);
    }

    /// <summary>A vehicle that does not exist and one owned by someone else look the same: 404 (design decision).</summary>
    private async Task<Vehicle> RequireOwnedAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        await vehicles.FindByIdAndUserIdAsync(vehicleId, currentUser.UserId, cancellationToken)
        ?? throw new NotFoundException("Vehicle");

    private async Task EnsureVinIsFreeAsync(Guid userId, string vin, Guid? excludingVehicleId, CancellationToken cancellationToken)
    {
        if (await vehicles.ExistsVinAsync(userId, Vehicle.NormalizeVin(vin), excludingVehicleId, cancellationToken))
        {
            throw new ConflictException(DuplicateVinMessage);
        }
    }
}
