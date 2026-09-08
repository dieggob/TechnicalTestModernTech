using FluentAssertions;
using Maintenance.Application.Time;
using Maintenance.Application.Vehicles;
using NSubstitute;

namespace Maintenance.UnitTests.Vehicles;

public class VehicleInputValidatorTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
    private readonly VehicleInputValidator _validator;

    public VehicleInputValidatorTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);
        _validator = new VehicleInputValidator(clock);
    }

    public static VehicleInput Valid() => new("Toyota", "Corolla", 2020, "1HGCM82633A004352", "ABC-123", 45000);

    [Fact]
    public void ValidInput_Passes() => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("Make", "")]
    [InlineData("Model", "")]
    [InlineData("Vin", "")]
    [InlineData("Vin", "NOT VALID VIN!")]
    [InlineData("Vin", "TOOLONGVINTOOLONGVIN")]
    [InlineData("LicensePlate", "")]
    public void InvalidText_FailsOnThatProperty(string property, string value)
    {
        var input = property switch
        {
            "Make" => Valid() with { Make = value },
            "Model" => Valid() with { Model = value },
            "Vin" => Valid() with { Vin = value },
            _ => Valid() with { LicensePlate = value },
        };

        _validator.Validate(input).Errors.Should().Contain(error => error.PropertyName == property);
    }

    [Theory]
    [InlineData(1885)]
    [InlineData(2028)]
    public void YearOutsideRange_Fails(int year) =>
        _validator.Validate(Valid() with { Year = year }).Errors.Should().Contain(error => error.PropertyName == "Year");

    [Fact]
    public void NextYear_IsAllowed() => _validator.Validate(Valid() with { Year = 2027 }).IsValid.Should().BeTrue();

    [Fact]
    public void NegativeMileage_Fails() =>
        _validator.Validate(Valid() with { CurrentMileage = -1 }).Errors.Should().Contain(error => error.PropertyName == "CurrentMileage");
}
