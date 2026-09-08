using FluentAssertions;
using Maintenance.Application.Maintenance;
using Maintenance.Application.Time;
using NSubstitute;

namespace Maintenance.UnitTests.MaintenanceRecords;

public class MaintenanceInputValidatorTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
    private readonly MaintenanceInputValidator _validator;

    public MaintenanceInputValidatorTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);
        _validator = new MaintenanceInputValidator(clock);
    }

    public static MaintenanceInput Valid() =>
        new("Oil change", 89.99m, new DateOnly(2026, 9, 1), 46000, "Quick Lube", "Synthetic 5W-30");

    [Fact]
    public void ValidInput_Passes() => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void OptionalFields_MayBeNull() =>
        _validator.Validate(Valid() with { ServiceProvider = null, Notes = null }).IsValid.Should().BeTrue();

    [Fact]
    public void BlankDescription_Fails() =>
        _validator.Validate(Valid() with { Description = "" }).Errors.Should().Contain(e => e.PropertyName == "Description");

    [Theory]
    [InlineData(-0.01)]
    [InlineData(10.123)]
    public void InvalidCost_Fails(double cost) =>
        _validator.Validate(Valid() with { CostUsd = (decimal)cost }).Errors.Should().Contain(e => e.PropertyName == "CostUsd");

    [Fact]
    public void TodayIsAllowed_TomorrowIsNot()
    {
        _validator.Validate(Valid() with { DatePerformed = new DateOnly(2026, 9, 8) }).IsValid.Should().BeTrue();
        _validator.Validate(Valid() with { DatePerformed = new DateOnly(2026, 9, 9) }).Errors.Should().Contain(e => e.PropertyName == "DatePerformed");
    }

    [Fact]
    public void NegativeMileage_Fails() =>
        _validator.Validate(Valid() with { MileageAtService = -1 }).Errors.Should().Contain(e => e.PropertyName == "MileageAtService");
}
