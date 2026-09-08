using FluentAssertions;
using Maintenance.Application.Auth;
using Maintenance.Application.Exceptions;
using Maintenance.Application.Maintenance;
using Maintenance.Application.Observability;
using Maintenance.Application.Persistence;
using Maintenance.Application.Time;
using Maintenance.Domain.Maintenance;
using Maintenance.Domain.Vehicles;
using NSubstitute;

namespace Maintenance.UnitTests.MaintenanceRecords;

public class MaintenanceServiceCreateTests
{
    private static readonly DateTime Now = new(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly IMaintenanceRecordRepository _records = Substitute.For<IMaintenanceRecordRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUnitOfWorkTransaction _transaction = Substitute.For<IUnitOfWorkTransaction>();
    private readonly IMaintenanceMetrics _metrics = Substitute.For<IMaintenanceMetrics>();
    private readonly Vehicle _vehicle = Vehicle.Create(UserId, new VehicleDetails("Toyota", "Corolla", 2020, "VIN1", "ABC", 45000), Now.AddDays(-1));
    private readonly MaintenanceService _service;

    public MaintenanceServiceCreateTests()
    {
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(Now);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(UserId);
        _vehicles.FindByIdAndUserIdAsync(_vehicle.Id, UserId, Arg.Any<CancellationToken>()).Returns(_vehicle);
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);
        _service = new MaintenanceService(_vehicles, _records, _unitOfWork, currentUser, clock, new MaintenanceInputValidator(clock), _metrics);
    }

    [Fact]
    public async Task Create_HigherMileage_StoresRecord_AdvancesVehicle_AndCommitsOnce()
    {
        var result = await _service.CreateAsync(_vehicle.Id, MaintenanceInputValidatorTests.Valid() with { MileageAtService = 47000 }, CancellationToken.None);

        await _records.Received(1).AddAsync(Arg.Is<MaintenanceRecord>(r => r.VehicleId == _vehicle.Id && r.Description == "Oil change"), Arg.Any<CancellationToken>());
        _vehicle.CurrentMileage.Should().Be(47000);
        result.VehicleCurrentMileage.Should().Be(47000);
        result.Record.CostUsd.Should().Be(89.99m);
        Received.InOrder(async () =>
        {
            await _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            await _transaction.CommitAsync(Arg.Any<CancellationToken>());
        });
        _metrics.Received(1).RecordCreated();
        _metrics.Received(1).MileageAdvanced();
    }

    [Fact]
    public async Task Create_LowerMileage_LeavesVehicleMileage_AndDoesNotCountAnAdvance()
    {
        var result = await _service.CreateAsync(_vehicle.Id, MaintenanceInputValidatorTests.Valid() with { MileageAtService = 30000 }, CancellationToken.None);

        _vehicle.CurrentMileage.Should().Be(45000);
        result.VehicleCurrentMileage.Should().Be(45000);
        _metrics.DidNotReceive().MileageAdvanced();
    }

    [Fact]
    public async Task Create_WhenTheRecordInsertFails_NeverCommits_SoTheVehicleChangeIsRolledBack()
    {
        _records.AddAsync(Arg.Any<MaintenanceRecord>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("disk full")));

        var act = () => _service.CreateAsync(_vehicle.Id, MaintenanceInputValidatorTests.Valid() with { MileageAtService = 47000 }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.Received(1).DisposeAsync();
        _metrics.DidNotReceive().RecordCreated();
    }

    [Fact]
    public async Task Create_AnotherUsersVehicle_Throws404_BeforeValidatingInput()
    {
        var act = () => _service.CreateAsync(Guid.NewGuid(), MaintenanceInputValidatorTests.Valid() with { Description = "" }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceiveWithAnyArgs().BeginTransactionAsync(default);
    }

    [Fact]
    public async Task Create_InvalidInput_ThrowsValidation_WithoutOpeningATransaction()
    {
        var act = () => _service.CreateAsync(_vehicle.Id, MaintenanceInputValidatorTests.Valid() with { Description = "" }, CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Should().ContainKey("description");
        await _unitOfWork.DidNotReceiveWithAnyArgs().BeginTransactionAsync(default);
    }
}
