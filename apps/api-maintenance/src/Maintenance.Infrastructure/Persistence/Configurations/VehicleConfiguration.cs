using Maintenance.Domain.Users;
using Maintenance.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maintenance.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Vehicle"/> to the VEHICLES table of the design.</summary>
public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles", table => table.HasCheckConstraint("ck_vehicles_current_mileage", "current_mileage >= 0"));
        builder.HasKey(vehicle => vehicle.Id);
        builder.Property(vehicle => vehicle.Id).HasColumnName("id");
        builder.Property(vehicle => vehicle.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(vehicle => vehicle.Make).HasColumnName("make").HasMaxLength(100).IsRequired();
        builder.Property(vehicle => vehicle.Model).HasColumnName("model").HasMaxLength(100).IsRequired();
        builder.Property(vehicle => vehicle.Year).HasColumnName("year").IsRequired();
        builder.Property(vehicle => vehicle.Vin).HasColumnName("vin").HasMaxLength(17).IsRequired();
        builder.Property(vehicle => vehicle.LicensePlate).HasColumnName("license_plate").HasMaxLength(20).IsRequired();
        builder.Property(vehicle => vehicle.CurrentMileage).HasColumnName("current_mileage").IsRequired();
        builder.Property(vehicle => vehicle.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(vehicle => vehicle.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(vehicle => vehicle.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(vehicle => vehicle.UserId).HasDatabaseName("ix_vehicles_user_id");
        builder.HasIndex(vehicle => new { vehicle.UserId, vehicle.Vin }).IsUnique().HasDatabaseName("ux_vehicles_user_vin");
    }
}
