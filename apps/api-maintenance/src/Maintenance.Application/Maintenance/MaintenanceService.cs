using FluentValidation;
using Maintenance.Application.Auth;
using Maintenance.Application.Exceptions;
using Maintenance.Application.Observability;
using Maintenance.Application.Persistence;
using Maintenance.Application.Time;
using Maintenance.Application.Validation;
using Maintenance.Domain.Maintenance;
using Maintenance.Domain.Vehicles;

namespace Maintenance.Application.Maintenance;

/// <summary>
/// Maintenance record use cases. Every operation first proves the caller owns the vehicle;
/// writes that touch the vehicle's mileage run in one transaction with the record.
/// </summary>
public sealed class MaintenanceService(
    IVehicleRepository vehicles,
    IMaintenanceRecordRepository records,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IValidator<MaintenanceInput> validator,
    IMaintenanceMetrics metrics)
{
    public async Task<MaintenanceWriteResult> CreateAsync(Guid vehicleId, MaintenanceInput input, CancellationToken cancellationToken)
    {
        input = input.Trimmed();
        var vehicle = await RequireOwnedVehicleAsync(vehicleId, cancellationToken);
        ValidationRunner.Validate(validator, input);

        var now = clock.UtcNow;
        var record = MaintenanceRecord.Create(vehicle.Id, input.ToDetails(), now);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await records.AddAsync(record, cancellationToken);
        var advanced = vehicle.AdvanceMileage(record.MileageAtService, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        metrics.RecordCreated();
        if (advanced)
        {
            metrics.MileageAdvanced();
        }

        return new MaintenanceWriteResult(MaintenanceRecordDto.From(record), vehicle.CurrentMileage);
    }

    /// <summary>The vehicle's history, newest first. The ownership check happens before the records are read.</summary>
    public async Task<IReadOnlyList<MaintenanceRecordDto>> ListByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await RequireOwnedVehicleAsync(vehicleId, cancellationToken);
        var history = await records.FindAllByVehicleIdAsync(vehicle.Id, cancellationToken);
        return history.Select(MaintenanceRecordDto.From).ToList();
    }

    /// <summary>A vehicle that does not exist and one owned by someone else look the same: 404 (design decision).</summary>
    private async Task<Vehicle> RequireOwnedVehicleAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        await vehicles.FindByIdAndUserIdAsync(vehicleId, currentUser.UserId, cancellationToken)
        ?? throw new NotFoundException("Vehicle");
}
