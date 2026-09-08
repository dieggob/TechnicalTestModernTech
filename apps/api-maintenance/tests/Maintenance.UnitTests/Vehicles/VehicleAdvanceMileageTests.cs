using FluentAssertions;
using Maintenance.Domain.Vehicles;

namespace Maintenance.UnitTests.Vehicles;

public class VehicleAdvanceMileageTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);

    private static Vehicle VehicleAt(int mileage) =>
        Vehicle.Create(Guid.NewGuid(), new VehicleDetails("Toyota", "Corolla", 2020, "VIN1", "ABC", mileage), Now.AddDays(-1));

    [Fact]
    public void HigherMileage_RaisesCurrentMileage_AndReportsIt()
    {
        var vehicle = VehicleAt(45000);

        var advanced = vehicle.AdvanceMileage(47000, Now);

        advanced.Should().BeTrue();
        vehicle.CurrentMileage.Should().Be(47000);
        vehicle.UpdatedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData(45000)]
    [InlineData(40000)]
    public void EqualOrLowerMileage_LeavesTheVehicleUnchanged(int mileageAtService)
    {
        var vehicle = VehicleAt(45000);
        var before = vehicle.UpdatedAt;

        var advanced = vehicle.AdvanceMileage(mileageAtService, Now);

        advanced.Should().BeFalse();
        vehicle.CurrentMileage.Should().Be(45000);
        vehicle.UpdatedAt.Should().Be(before);
    }
}
