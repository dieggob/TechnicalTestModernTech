using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maintenance.Application.Vehicles;

namespace Maintenance.IntegrationTests.Vehicles;

public class ReadVehicleTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ReadVehicleTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_ReturnsOnlyTheCallersVehicles()
    {
        var alice = await _factory.AuthenticatedClientAsync("alice@example.com");
        var bob = await _factory.AuthenticatedClientAsync("bob@example.com");
        await alice.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("ALICE00000000001"));
        await alice.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("ALICE00000000002") with { Make = "Honda" });
        await bob.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("BOB0000000000001"));

        var alicesList = (await alice.GetFromJsonAsync<List<VehicleDto>>(VehicleFlows.VehiclesUrl))!;
        var bobsList = (await bob.GetFromJsonAsync<List<VehicleDto>>(VehicleFlows.VehiclesUrl))!;

        alicesList.Select(v => v.Vin).Should().BeEquivalentTo("ALICE00000000001", "ALICE00000000002");
        alicesList.Select(v => v.Make).Should().ContainInOrder("Honda", "Toyota");
        bobsList.Select(v => v.Vin).Should().BeEquivalentTo("BOB0000000000001");
    }

    [Fact]
    public async Task Get_OwnVehicle_Returns200_AndOthersOrUnknownReturn404()
    {
        var owner = await _factory.AuthenticatedClientAsync("get-owner@example.com");
        var stranger = await _factory.AuthenticatedClientAsync("get-stranger@example.com");
        var vehicle = await owner.CreateVehicleAndReadAsync(VehicleFlows.SampleVehicle("OWNER00000000001"));

        var own = await owner.GetAsync($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}");
        var theirs = await stranger.GetAsync($"{VehicleFlows.VehiclesUrl}/{vehicle.Id}");
        var unknown = await owner.GetAsync($"{VehicleFlows.VehiclesUrl}/{Guid.NewGuid()}");

        own.StatusCode.Should().Be(HttpStatusCode.OK);
        (await own.Content.ReadFromJsonAsync<VehicleDto>())!.Vin.Should().Be("OWNER00000000001");
        theirs.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await theirs.Content.ReadAsStringAsync()).Should().Be(await unknown.Content.ReadAsStringAsync(), "ownership is never revealed");
    }

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync(VehicleFlows.VehiclesUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
