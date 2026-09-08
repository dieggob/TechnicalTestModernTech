using Maintenance.Domain.Maintenance;
using Maintenance.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="MaintenanceRecord"/> to the MAINTENANCE_RECORDS table of the design.
/// SQLite has no decimal type, so cost_usd is stored as TEXT by the provider and the check
/// constraint compares a cast (implementation design risk).
/// </summary>
public sealed class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("maintenance_records", table =>
        {
            table.HasCheckConstraint("ck_maintenance_records_cost_usd", "CAST(cost_usd AS REAL) >= 0");
            table.HasCheckConstraint("ck_maintenance_records_mileage", "mileage_at_service >= 0");
        });
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).HasColumnName("id");
        builder.Property(record => record.VehicleId).HasColumnName("vehicle_id").IsRequired();
        builder.Property(record => record.Description).HasColumnName("description").HasMaxLength(200).IsRequired();
        builder.Property(record => record.CostUsd).HasColumnName("cost_usd").HasPrecision(12, 2).IsRequired();
        builder.Property(record => record.DatePerformed).HasColumnName("date_performed").IsRequired();
        builder.Property(record => record.MileageAtService).HasColumnName("mileage_at_service").IsRequired();
        builder.Property(record => record.ServiceProvider).HasColumnName("service_provider").HasMaxLength(150);
        builder.Property(record => record.Notes).HasColumnName("notes").HasMaxLength(2000);
        builder.Property(record => record.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(record => record.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<Vehicle>().WithMany().HasForeignKey(record => record.VehicleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(record => new { record.VehicleId, record.DatePerformed }).HasDatabaseName("ix_maintenance_vehicle_date");
    }
}
