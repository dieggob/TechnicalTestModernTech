using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maintenance.Application.Maintenance;
using Maintenance.Application.Vehicles;
using Maintenance.Infrastructure.Persistence;
using Maintenance.IntegrationTests.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Maintenance.IntegrationTests.MaintenanceRecords;

public class DeleteRecordTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public DeleteRecordTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Clock.UtcNow = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task Delete_Returns204_RemovesTheRecord_AndLeavesTheVehicleMileage()
    {
        var client = await _factory.AuthenticatedClientAsync("del-rec@example.com");
        var vehicle = await client.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("DLR0000000000001") with { CurrentMileage = 45000 });
        var created = await client.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord(mileageAtService: 48000));

        var response = await client.DeleteAsync(MaintenanceFlows.RecordUrl(vehicle.Id, created.Record.Id));
        var history = await client.GetFromJsonAsync<List<MaintenanceRecordDto>>(MaintenanceFlows.RecordsUrl(vehicle.Id));
        var afterwards = await client.GetFromJsonAsync<VehicleDto>($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        history.Should().BeEmpty();
        afterwards!.CurrentMileage.Should().Be(48000, "deleting a record never lowers the odometer reading");
    }

    [Fact]
    public async Task Delete_ForeignOrUnknown_Returns404()
    {
        var owner = await _factory.AuthenticatedClientAsync("del-rec-o@example.com");
        var stranger = await _factory.AuthenticatedClientAsync("del-rec-s@example.com");
        var vehicle = await owner.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("DLR0000000000002"));
        var created = await owner.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord());

        var foreign = await stranger.DeleteAsync(MaintenanceFlows.RecordUrl(vehicle.Id, created.Record.Id));
        var unknown = await owner.DeleteAsync(MaintenanceFlows.RecordUrl(vehicle.Id, Guid.NewGuid()));

        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletingTheVehicle_CascadesToItsRecords()
    {
        var client = await _factory.AuthenticatedClientAsync("del-cascade@example.com");
        var vehicle = await client.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("DLR0000000000003"));
        await client.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord());
        await client.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord(description: "Brakes"));

        var deleted = await client.DeleteAsync($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>();
        (await context.MaintenanceRecords.CountAsync(record => record.VehicleId == vehicle.Id)).Should().Be(0);
    }
}
