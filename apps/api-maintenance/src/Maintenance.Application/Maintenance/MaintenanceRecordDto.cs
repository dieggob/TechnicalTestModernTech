using Maintenance.Domain.Maintenance;

namespace Maintenance.Application.Maintenance;

public sealed record MaintenanceRecordDto(
    Guid Id,
    Guid VehicleId,
    string Description,
    decimal CostUsd,
    DateOnly DatePerformed,
    int MileageAtService,
    string? ServiceProvider,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static MaintenanceRecordDto From(MaintenanceRecord record) => new(
        record.Id, record.VehicleId, record.Description, record.CostUsd, record.DatePerformed, record.MileageAtService,
        record.ServiceProvider, record.Notes, record.CreatedAt, record.UpdatedAt);
}

/// <summary>
/// Create and update return the vehicle's resulting mileage too, so the client refreshes it
/// without a second call (design API Contract).
/// </summary>
public sealed record MaintenanceWriteResult(MaintenanceRecordDto Record, int VehicleCurrentMileage);
