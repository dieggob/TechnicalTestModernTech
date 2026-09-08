using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maintenance.Application.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.IntegrationTests.Vehicles;

public class CreateVehicleTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CreateVehicleTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ValidVehicle_Returns201WithTheVehicle()
    {
        var client = await _factory.AuthenticatedClientAsync("owner@example.com");

        var response = await client.CreateVehicleAsync(VehicleFlows.SampleVehicle());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var vehicle = await response.Content.ReadFromJsonAsync<VehicleDto>();
        vehicle!.Id.Should().NotBeEmpty();
        vehicle.Vin.Should().Be("1HGCM82633A004352");
        response.Headers.Location!.ToString().Should().EndWith($"/api/v1/vehicles/{vehicle.Id}");
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().CreateVehicleAsync(VehicleFlows.SampleVehicle());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_InvalidInput_Returns400WithFieldErrors()
    {
        var client = await _factory.AuthenticatedClientAsync("invalid@example.com");

        var response = await client.CreateVehicleAsync(VehicleFlows.SampleVehicle() with { Year = 1700, CurrentMileage = -5, Vin = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKeys("year", "currentMileage", "vin");
    }

    [Fact]
    public async Task Create_SameVinTwiceForOneUser_Returns409_ButAnotherUserMayRegisterIt()
    {
        var first = await _factory.AuthenticatedClientAsync("first@example.com");
        var second = await _factory.AuthenticatedClientAsync("second@example.com");
        await first.CreateVehicleAsync(VehicleFlows.SampleVehicle("WVWZZZ1JZXW000001"));

        var duplicate = await first.CreateVehicleAsync(VehicleFlows.SampleVehicle("wvwzzz1jzxw000001"));
        var otherUser = await second.CreateVehicleAsync(VehicleFlows.SampleVehicle("WVWZZZ1JZXW000001"));

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await duplicate.Content.ReadFromJsonAsync<ProblemDetails>())!.Title.Should().Be(VehicleService.DuplicateVinMessage);
        otherUser.StatusCode.Should().Be(HttpStatusCode.Created, "VIN uniqueness is per user (product decision)");
    }
}
