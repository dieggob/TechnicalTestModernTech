using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maintenance.Application.Maintenance;
using Maintenance.IntegrationTests.Vehicles;

namespace Maintenance.IntegrationTests.MaintenanceRecords;

public class ListRecordsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ListRecordsTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Clock.UtcNow = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task List_ReturnsTheVehiclesRecordsNewestFirst()
    {
        var client = await _factory.AuthenticatedClientAsync("hist-owner@example.com");
        var vehicle = await client.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("HIS0000000000001"));
        await client.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord(description: "Middle") with { DatePerformed = new DateOnly(2026, 6, 1) });
        await client.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord(description: "Newest") with { DatePerformed = new DateOnly(2026, 9, 1) });
        await client.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord(description: "Oldest") with { DatePerformed = new DateOnly(2026, 1, 1) });

        var history = (await client.GetFromJsonAsync<List<MaintenanceRecordDto>>(MaintenanceFlows.RecordsUrl(vehicle.Id)))!;

        history.Select(record => record.Description).Should().ContainInOrder("Newest", "Middle", "Oldest");
        history.Should().OnlyContain(record => record.VehicleId == vehicle.Id);
    }

    [Fact]
    public async Task List_EmptyHistory_ReturnsEmptyArray()
    {
        var client = await _factory.AuthenticatedClientAsync("hist-empty@example.com");
        var vehicle = await client.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("HIS0000000000002"));

        var history = await client.GetFromJsonAsync<List<MaintenanceRecordDto>>(MaintenanceFlows.RecordsUrl(vehicle.Id));

        history.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ForAnotherUsersVehicle_Returns404()
    {
        var owner = await _factory.AuthenticatedClientAsync("hist-o@example.com");
        var stranger = await _factory.AuthenticatedClientAsync("hist-s@example.com");
        var vehicle = await owner.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("HIS0000000000003"));
        await owner.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord());

        var response = await stranger.GetAsync(MaintenanceFlows.RecordsUrl(vehicle.Id));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
