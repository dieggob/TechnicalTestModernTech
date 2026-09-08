namespace Maintenance.Domain.Vehicles;

/// <summary>
/// A vehicle owned by one user. Owns its invariants: the VIN is normalised once here, and
/// identifying details change only through <see cref="Update"/>.
/// </summary>
public sealed class Vehicle
{
    private Vehicle()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Make { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public int Year { get; private set; }
    public string Vin { get; private set; } = null!;
    public string LicensePlate { get; private set; } = null!;
    public int CurrentMileage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static Vehicle Create(Guid userId, VehicleDetails details, DateTime now)
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = userId, CreatedAt = now };
        vehicle.Apply(details, now);
        return vehicle;
    }

    public void Update(VehicleDetails details, DateTime now) => Apply(details, now);

    /// <summary>
    /// A maintenance record with a higher mileage than the odometer reading known so far raises it
    /// (product decision). Lower or equal readings never lower it: a back-dated job says nothing
    /// about today's odometer. Returns whether the mileage changed.
    /// </summary>
    public bool AdvanceMileage(int mileageAtService, DateTime now)
    {
        if (mileageAtService <= CurrentMileage)
        {
            return false;
        }

        CurrentMileage = mileageAtService;
        UpdatedAt = now;
        return true;
    }

    /// <summary>Trimmed and upper-cased, so the per-user unique index sees one form of each VIN.</summary>
    public static string NormalizeVin(string vin) => vin.Trim().ToUpperInvariant();

    private void Apply(VehicleDetails details, DateTime now)
    {
        Make = details.Make.Trim();
        Model = details.Model.Trim();
        Year = details.Year;
        Vin = NormalizeVin(details.Vin);
        LicensePlate = details.LicensePlate.Trim().ToUpperInvariant();
        CurrentMileage = details.CurrentMileage;
        UpdatedAt = now;
    }
}

/// <summary>The identifying details a user records for a vehicle (design: make, model, year, VIN, plate, mileage).</summary>
public sealed record VehicleDetails(string Make, string Model, int Year, string Vin, string LicensePlate, int CurrentMileage);
