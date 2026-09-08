using FluentAssertions;
using Maintenance.Application.Auth;
using Maintenance.Application.Exceptions;
using Maintenance.Application.Observability;
using Maintenance.Application.Time;
using Maintenance.Application.Vehicles;
using Maintenance.Domain.Vehicles;
using NSubstitute;

namespace Maintenance.UnitTests.Vehicles;

public class VehicleServiceCreateTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly IMaintenanceMetrics _metrics = Substitute.For<IMaintenanceMetrics>();
    private readonly VehicleService _service;

    public VehicleServiceCreateTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(UserId);
        _service = new VehicleService(_vehicles, currentUser, clock, new VehicleInputValidator(clock), _metrics);
    }

    [Fact]
    public async Task Create_ValidInput_StoresNormalisedVehicleForTheCaller()
    {
        Vehicle? added = null;
        _vehicles.AddAsync(Arg.Do<Vehicle>(vehicle => added = vehicle), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var dto = await _service.CreateAsync(VehicleInputValidatorTests.Valid() with { Vin = " 1hgcm82633a004352 ", LicensePlate = " abc-123 " }, CancellationToken.None);

        added!.UserId.Should().Be(UserId);
        added.Vin.Should().Be("1HGCM82633A004352");
        added.LicensePlate.Should().Be("ABC-123");
        added.CreatedAt.Should().Be(Now);
        dto.Id.Should().Be(added.Id);
        await _vehicles.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _metrics.Received(1).VehicleCreated();
    }

    [Fact]
    public async Task Create_DuplicateVinForTheSameUser_ThrowsConflict()
    {
        _vehicles.ExistsVinAsync(UserId, "1HGCM82633A004352", null, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => _service.CreateAsync(VehicleInputValidatorTests.Valid(), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).WithMessage(VehicleService.DuplicateVinMessage);
        await _vehicles.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Create_InvalidInput_ThrowsValidation_BeforeTouchingTheRepository()
    {
        var act = () => _service.CreateAsync(VehicleInputValidatorTests.Valid() with { Year = 1800 }, CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("year");
        await _vehicles.DidNotReceiveWithAnyArgs().ExistsVinAsync(default, default!, default, default);
    }
}
