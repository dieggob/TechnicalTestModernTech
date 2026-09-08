namespace Maintenance.Domain.Maintenance;

/// <summary>
/// Records are always reached through their vehicle, and the vehicle through its owner, so a
/// record id from another user's vehicle is never found.
/// </summary>
public interface IMaintenanceRecordRepository
{
    Task<MaintenanceRecord?> FindByIdAndVehicleIdAsync(Guid recordId, Guid vehicleId, CancellationToken cancellationToken);

    /// <summary>Newest first by date performed, then by creation.</summary>
    Task<IReadOnlyList<MaintenanceRecord>> FindAllByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken);

    Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken);

    void Remove(MaintenanceRecord record);
}
