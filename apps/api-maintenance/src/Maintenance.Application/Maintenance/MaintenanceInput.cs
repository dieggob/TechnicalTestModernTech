using FluentValidation;
using Maintenance.Application.Time;
using Maintenance.Domain.Maintenance;

namespace Maintenance.Application.Maintenance;

/// <summary>Request body for creating and updating a maintenance record (design API Contract: MaintenanceInput).</summary>
public sealed record MaintenanceInput(
    string Description,
    decimal CostUsd,
    DateOnly DatePerformed,
    int MileageAtService,
    string? ServiceProvider,
    string? Notes)
{
    public MaintenanceInput Trimmed() => this with
    {
        Description = Description?.Trim() ?? string.Empty,
        ServiceProvider = string.IsNullOrWhiteSpace(ServiceProvider) ? null : ServiceProvider.Trim(),
        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
    };

    public MaintenanceDetails ToDetails() => new(Description, CostUsd, DatePerformed, MileageAtService, ServiceProvider, Notes);
}

public sealed class MaintenanceInputValidator : AbstractValidator<MaintenanceInput>
{
    public const int MaximumDescriptionLength = 200;
    public const int MaximumProviderLength = 150;
    public const int MaximumNotesLength = 2000;

    public MaintenanceInputValidator(IClock clock)
    {
        RuleFor(input => input.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(MaximumDescriptionLength);
        RuleFor(input => input.CostUsd)
            .GreaterThanOrEqualTo(0).WithMessage("Cost cannot be negative.")
            .PrecisionScale(12, 2, ignoreTrailingZeros: true).WithMessage("Cost must have at most two decimals.");
        RuleFor(input => input.DatePerformed)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(clock.UtcNow)).WithMessage("Date performed cannot be in the future.");
        RuleFor(input => input.MileageAtService).GreaterThanOrEqualTo(0).WithMessage("Mileage cannot be negative.");
        RuleFor(input => input.ServiceProvider).MaximumLength(MaximumProviderLength);
        RuleFor(input => input.Notes).MaximumLength(MaximumNotesLength);
    }
}
