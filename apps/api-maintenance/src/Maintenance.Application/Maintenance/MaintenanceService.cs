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
        var result = await WriteAndAdvanceAsync(vehicle, record, now,
            () => records.AddAsync(record, cancellationToken), cancellationToken);
        metrics.RecordCreated();
        return result;
    }

    /// <summary>Editing applies the same validation and the same mileage rule as creation.</summary>
    public async Task<MaintenanceWriteResult> UpdateAsync(Guid vehicleId, Guid recordId, MaintenanceInput input, CancellationToken cancellationToken)
    {
        input = input.Trimmed();
        var vehicle = await RequireOwnedVehicleAsync(vehicleId, cancellationToken);
        var record = await RequireRecordAsync(vehicle, recordId, cancellationToken);
        ValidationRunner.Validate(validator, input);

        var now = clock.UtcNow;
        record.Update(input.ToDetails(), now);
        var result = await WriteAndAdvanceAsync(vehicle, record, now, () => Task.CompletedTask, cancellationToken);
        metrics.RecordUpdated();
        return result;
    }

    /// <summary>Removes a record. The vehicle's mileage is never lowered: the odometer reading was real (design decision).</summary>
    public async Task DeleteAsync(Guid vehicleId, Guid recordId, CancellationToken cancellationToken)
    {
        var vehicle = await RequireOwnedVehicleAsync(vehicleId, cancellationToken);
        var record = await RequireRecordAsync(vehicle, recordId, cancellationToken);

        records.Remove(record);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        metrics.RecordDeleted();
    }

    /// <summary>
    /// The one place a record write and the vehicle's mileage advance are committed together
    /// (design decision: they can never disagree after a partial failure).
    /// </summary>
    private async Task<MaintenanceWriteResult> WriteAndAdvanceAsync(
        Vehicle vehicle, MaintenanceRecord record, DateTime now, Func<Task> persistRecord, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await persistRecord();
        var advanced = vehicle.AdvanceMileage(record.MileageAtService, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        if (advanced)
        {
            metrics.MileageAdvanced();
        }

        return new MaintenanceWriteResult(MaintenanceRecordDto.From(record), vehicle.CurrentMileage);
    }

    /// <summary>A record is found only under its own vehicle, which was already proven to be the caller's.</summary>
    private async Task<MaintenanceRecord> RequireRecordAsync(Vehicle vehicle, Guid recordId, CancellationToken cancellationToken) =>
        await records.FindByIdAndVehicleIdAsync(recordId, vehicle.Id, cancellationToken)
        ?? throw new NotFoundException("Maintenance record");

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
