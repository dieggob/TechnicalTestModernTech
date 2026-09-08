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

    private async Task EnsureVinIsFreeAsync(Guid userId, string vin, Guid? excludingVehicleId, CancellationToken cancellationToken)
    {
        if (await vehicles.ExistsVinAsync(userId, Vehicle.NormalizeVin(vin), excludingVehicleId, cancellationToken))
        {
            throw new ConflictException(DuplicateVinMessage);
        }
    }
}
