namespace Maintenance.Domain.Maintenance;

/// <summary>
/// One maintenance job logged against a vehicle. The type of work is free text (design decision:
/// no predefined categories); cost is always US dollars.
/// </summary>
public sealed class MaintenanceRecord
{
    private MaintenanceRecord()
    {
    }

    public Guid Id { get; private set; }
    public Guid VehicleId { get; private set; }
    public string Description { get; private set; } = null!;
    public decimal CostUsd { get; private set; }
    public DateOnly DatePerformed { get; private set; }
    public int MileageAtService { get; private set; }
    public string? ServiceProvider { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static MaintenanceRecord Create(Guid vehicleId, MaintenanceDetails details, DateTime now)
    {
        var record = new MaintenanceRecord { Id = Guid.NewGuid(), VehicleId = vehicleId, CreatedAt = now };
        record.Apply(details, now);
        return record;
    }

    public void Update(MaintenanceDetails details, DateTime now) => Apply(details, now);

    private void Apply(MaintenanceDetails details, DateTime now)
    {
        Description = details.Description.Trim();
        CostUsd = decimal.Round(details.CostUsd, 2, MidpointRounding.ToEven);
        DatePerformed = details.DatePerformed;
        MileageAtService = details.MileageAtService;
        ServiceProvider = NullIfBlank(details.ServiceProvider);
        Notes = NullIfBlank(details.Notes);
        UpdatedAt = now;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>What a user records for a job: cost, date, and mileage are mandatory; provider and notes optional.</summary>
public sealed record MaintenanceDetails(
    string Description,
    decimal CostUsd,
    DateOnly DatePerformed,
    int MileageAtService,
    string? ServiceProvider,
    string? Notes);
