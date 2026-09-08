using FluentValidation;
using Maintenance.Application.Time;
using Maintenance.Domain.Vehicles;

namespace Maintenance.Application.Vehicles;

/// <summary>Request body for creating and updating a vehicle (design API Contract: VehicleInput).</summary>
public sealed record VehicleInput(string Make, string Model, int Year, string Vin, string LicensePlate, int CurrentMileage)
{
    /// <summary>Whitespace around typed values is never meaningful; trim once before validating.</summary>
    public VehicleInput Trimmed() => this with
    {
        Make = Make?.Trim() ?? string.Empty,
        Model = Model?.Trim() ?? string.Empty,
        Vin = Vin?.Trim() ?? string.Empty,
        LicensePlate = LicensePlate?.Trim() ?? string.Empty,
    };

    public VehicleDetails ToDetails() => new(Make, Model, Year, Vin, LicensePlate, CurrentMileage);
}

public sealed class VehicleInputValidator : AbstractValidator<VehicleInput>
{
    public const int FirstCarYear = 1886;
    public const int MaximumVinLength = 17;

    public VehicleInputValidator(IClock clock)
    {
        RuleFor(input => input.Make).NotEmpty().WithMessage("Make is required.").MaximumLength(100);
        RuleFor(input => input.Model).NotEmpty().WithMessage("Model is required.").MaximumLength(100);
        RuleFor(input => input.Year)
            .InclusiveBetween(FirstCarYear, clock.UtcNow.Year + 1)
            .WithMessage(input => $"Year must be between {FirstCarYear} and {clock.UtcNow.Year + 1}.");
        RuleFor(input => input.Vin)
            .NotEmpty().WithMessage("VIN is required.")
            .MaximumLength(MaximumVinLength).WithMessage($"VIN must be at most {MaximumVinLength} characters.")
            .Matches("^[A-Za-z0-9]+$").WithMessage("VIN must contain only letters and digits.");
        RuleFor(input => input.LicensePlate).NotEmpty().WithMessage("License plate is required.").MaximumLength(20);
        RuleFor(input => input.CurrentMileage).GreaterThanOrEqualTo(0).WithMessage("Mileage cannot be negative.");
    }
}
