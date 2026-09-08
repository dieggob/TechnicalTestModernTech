using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maintenance.Application.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.IntegrationTests.Vehicles;

public class UpdateDeleteVehicleTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public UpdateDeleteVehicleTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Update_OwnVehicle_Returns200WithNewValues()
    {
        var client = await _factory.AuthenticatedClientAsync("upd-owner@example.com");
        var vehicle = await client.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("UPD0000000000001"));

        var response = await client.PutAsJsonAsync($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}",
            VehicleFlows.SampleVehicle("UPD0000000000001") with { Model = "Camry", CurrentMileage = 50000 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<VehicleDto>();
        updated!.Model.Should().Be("Camry");
        updated.CurrentMileage.Should().Be(50000);
        updated.UpdatedAt.Should().BeOnOrAfter(vehicle.UpdatedAt);
    }

    [Fact]
    public async Task Update_ToAnotherOwnVehiclesVin_Returns409_ButKeepingItsOwnVinIsFine()
    {
        var client = await _factory.AuthenticatedClientAsync("upd-vin@example.com");
        var first = await client.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("VIN0000000000001"));
        await client.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("VIN0000000000002"));

        var collision = await client.PutAsJsonAsync($"{VehicleFlows.VehiclesUrl}/{first.Id}", VehicleFlows.SampleVehicle("VIN0000000000002"));
        var sameVin = await client.PutAsJsonAsync($"{VehicleFlows.VehiclesUrl}/{first.Id}", VehicleFlows.SampleVehicle("VIN0000000000001") with { Make = "Honda" });

        collision.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await collision.Content.ReadFromJsonAsync<ProblemDetails>())!.Title.Should().Be(VehicleService.DuplicateVinMessage);
        sameVin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Update_AnotherUsersVehicle_Returns404_AndInvalidInput_Returns400()
    {
        var owner = await _factory.AuthenticatedClientAsync("upd-o@example.com");
        var stranger = await _factory.AuthenticatedClientAsync("upd-s@example.com");
        var vehicle = await owner.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("STR0000000000001"));

        var foreign = await stranger.PutAsJsonAsync($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}", VehicleFlows.SampleVehicle("STR0000000000001"));
        var invalid = await owner.PutAsJsonAsync($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}", VehicleFlows.SampleVehicle("STR0000000000001") with { CurrentMileage = -1 });

        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_OwnVehicle_Returns204ThenGetReturns404_AndOthersCannotDelete()
    {
        var owner = await _factory.AuthenticatedClientAsync("del-o@example.com");
        var stranger = await _factory.AuthenticatedClientAsync("del-s@example.com");
        var vehicle = await owner.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("DEL0000000000001"));

        var foreign = await stranger.DeleteAsync($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}");
        var deleted = await owner.DeleteAsync($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}");
        var afterwards = await owner.GetAsync($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}");

        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        afterwards.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
