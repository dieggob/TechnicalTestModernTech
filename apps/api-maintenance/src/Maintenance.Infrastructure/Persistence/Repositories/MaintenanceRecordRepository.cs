using Maintenance.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace Maintenance.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceRecordRepository(MaintenanceDbContext context) : IMaintenanceRecordRepository
{
    public Task<MaintenanceRecord?> FindByIdAndVehicleIdAsync(Guid recordId, Guid vehicleId, CancellationToken cancellationToken) =>
        context.MaintenanceRecords.SingleOrDefaultAsync(record => record.Id == recordId && record.VehicleId == vehicleId, cancellationToken);

    public async Task<IReadOnlyList<MaintenanceRecord>> FindAllByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        await context.MaintenanceRecords
            .Where(record => record.VehicleId == vehicleId)
            .OrderByDescending(record => record.DatePerformed).ThenByDescending(record => record.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken) =>
        await context.MaintenanceRecords.AddAsync(record, cancellationToken);

    public void Remove(MaintenanceRecord record) => context.MaintenanceRecords.Remove(record);
}
