using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maintenance.Application.Maintenance;
using Maintenance.Application.Vehicles;
using Maintenance.IntegrationTests.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.IntegrationTests.MaintenanceRecords;

public class CreateRecordTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CreateRecordTests(ApiFactory factory)
    {
        _factory = factory;
        _factory.Clock.UtcNow = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task Create_Returns201WithTheRecordAndTheVehiclesResultingMileage()
    {
        var client = await _factory.AuthenticatedClientAsync("rec-owner@example.com");
        var vehicle = await client.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("REC0000000000001") with { CurrentMileage = 45000 });

        var response = await client.CreateRecordAsync(vehicle.Id, MaintenanceFlows.SampleRecord(mileageAtService: 47000));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<MaintenanceWriteResult>();
        result!.Record.Description.Should().Be("Oil change");
        result.Record.CostUsd.Should().Be(89.99m);
        result.Record.DatePerformed.Should().Be(new DateOnly(2026, 9, 1));
        result.Record.ServiceProvider.Should().Be("Quick Lube");
        result.VehicleCurrentMileage.Should().Be(47000, "the record's mileage exceeded the vehicle's");
        response.Headers.Location!.ToString().Should().EndWith($"/maintenance/{result.Record.Id}");
    }

    [Fact]
    public async Task Create_HigherMileage_UpdatesTheStoredVehicle_ButLowerMileageDoesNot()
    {
        var client = await _factory.AuthenticatedClientAsync("rec-mileage@example.com");
        var vehicle = await client.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("REC0000000000002") with { CurrentMileage = 45000 });

        await client.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord(mileageAtService: 48000));
        var afterHigher = await client.GetFromJsonAsync<VehicleDto>($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}");
        var lower = await client.CreateRecordAndReadAsync(vehicle.Id, MaintenanceFlows.SampleRecord(mileageAtService: 20000, description: "Back-dated tyre swap"));
        var afterLower = await client.GetFromJsonAsync<VehicleDto>($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}");

        afterHigher!.CurrentMileage.Should().Be(48000);
        lower.VehicleCurrentMileage.Should().Be(48000);
        afterLower!.CurrentMileage.Should().Be(48000);
    }

    [Fact]
    public async Task Create_InvalidInput_Returns400WithFieldErrors()
    {
        var client = await _factory.AuthenticatedClientAsync("rec-invalid@example.com");
        var vehicle = await client.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("REC0000000000003"));

        var response = await client.CreateRecordAsync(vehicle.Id,
            MaintenanceFlows.SampleRecord() with { Description = "  ", CostUsd = -1, DatePerformed = new DateOnly(2027, 1, 1), MileageAtService = -5 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKeys("description", "costUsd", "datePerformed", "mileageAtService");
    }

    [Fact]
    public async Task Create_ForAnotherUsersVehicle_Returns404()
    {
        var owner = await _factory.AuthenticatedClientAsync("rec-o@example.com");
        var stranger = await _factory.AuthenticatedClientAsync("rec-s@example.com");
        var vehicle = await owner.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("REC0000000000004"));

        var response = await stranger.CreateRecordAsync(vehicle.Id, MaintenanceFlows.SampleRecord());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().CreateRecordAsync(Guid.NewGuid(), MaintenanceFlows.SampleRecord());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
