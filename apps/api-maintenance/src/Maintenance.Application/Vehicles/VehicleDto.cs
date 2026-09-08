using Maintenance.Domain.Vehicles;

namespace Maintenance.Application.Vehicles;

/// <summary>Response shape for a vehicle: the design's attributes minus the owner, who is the caller.</summary>
public sealed record VehicleDto(
    Guid Id,
    string Make,
    string Model,
    int Year,
    string Vin,
    string LicensePlate,
    int CurrentMileage,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static VehicleDto From(Vehicle vehicle) => new(
        vehicle.Id, vehicle.Make, vehicle.Model, vehicle.Year, vehicle.Vin, vehicle.LicensePlate,
        vehicle.CurrentMileage, vehicle.CreatedAt, vehicle.UpdatedAt);
}
