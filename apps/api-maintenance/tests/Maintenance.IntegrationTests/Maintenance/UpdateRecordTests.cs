using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maintenance.Application.Maintenance;
using Maintenance.Application.Vehicles;
using Maintenance.IntegrationTests.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.IntegrationTests.MaintenanceRecords;

public class UpdateRecordTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public UpdateRecordTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Clock.UtcNow = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task Update_Returns200WithNewValues_AndAdvancesMileageWhenHigher()
    {
        var client = await _factory.AuthenticatedClientAsync("upd-rec@example.com");
        var vehicle = await client.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("UPR0000000000001") with { CurrentMileage = 45000 });
        var created = await client.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord(mileageAtService: 46000));

        var response = await client.PutAsJsonAsync(MaintenanceFlows.RecordUrl(vehicle.Id, created.Record.Id),
            MaintenanceFlows.SampleRecord(mileageAtService: 49000, description: "Oil change and filter") with { CostUsd = 120m, Notes = null });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MaintenanceWriteResult>();
        result!.Record.Id.Should().Be(created.Record.Id);
        result.Record.Description.Should().Be("Oil change and filter");
        result.Record.CostUsd.Should().Be(120m);
        result.Record.Notes.Should().BeNull();
        result.VehicleCurrentMileage.Should().Be(49000);
        (await client.GetFromJsonAsync<VehicleDto>($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}"))!.CurrentMileage.Should().Be(49000);
    }

    [Fact]
    public async Task Update_InvalidInput_Returns400_AndLeavesTheRecordUnchanged()
    {
        var client = await _factory.AuthenticatedClientAsync("upd-rec-invalid@example.com");
        var vehicle = await client.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("UPR0000000000002"));
        var created = await client.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord());

        var response = await client.PutAsJsonAsync(MaintenanceFlows.RecordUrl(vehicle.Id, created.Record.Id), MaintenanceFlows.SampleRecord() with { CostUsd = -3 });
        var history = await client.GetFromJsonAsync<List<MaintenanceRecordDto>>(MaintenanceFlows.RecordsUrl(vehicle.Id));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Should().ContainKey("costUsd");
        history!.Single().CostUsd.Should().Be(89.99m);
    }

    [Fact]
    public async Task Update_WrongVehicleOrOtherUser_Returns404()
    {
        var owner = await _factory.AuthenticatedClientAsync("upd-rec-o@example.com");
        var stranger = await _factory.AuthenticatedClientAsync("upd-rec-s@example.com");
        var vehicle = await owner.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("UPR0000000000003"));
        var otherVehicle = await owner.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("UPR0000000000004"));
        var created = await owner.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord());

        var wrongVehicle = await owner.PutAsJsonAsync(MaintenanceFlows.RecordUrl(otherVehicle.Id, created.Record.Id), MaintenanceFlows.SampleRecord());
        var foreign = await stranger.PutAsJsonAsync(MaintenanceFlows.RecordUrl(vehicle.Id, created.Record.Id), MaintenanceFlows.SampleRecord());
        var unknown = await owner.PutAsJsonAsync(MaintenanceFlows.RecordUrl(vehicle.Id, Guid.NewGuid()), MaintenanceFlows.SampleRecord());

        wrongVehicle.StatusCode.Should().Be(HttpStatusCode.NotFound);
        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
