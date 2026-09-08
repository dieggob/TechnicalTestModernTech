using System.Net.Http.Json;
using Maintenance.Application.Vehicles;
using Maintenance.IntegrationTests.Auth;

namespace Maintenance.IntegrationTests.Vehicles;

/// <summary>Shared steps for vehicle journeys: an authenticated client and a registered vehicle.</summary>
internal static class VehicleFlows
{
    public const string VehiclesUrl = "/api/v1/vehicles";

    public static VehicleInput SampleVehicle(string vin = "1HGCM82633A004352") =>
        new("Toyota", "Corolla", 2020, vin, "ABC-123", 45000);

    /// <summary>A client whose bearer token belongs to a freshly registered account.</summary>
    public static async Task<HttpClient> AuthenticatedClientAsync(this ApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        client.WithBearer(await client.LoginTokenAsync(email));
        return client;
    }

    public static Task<HttpResponseMessage> CreateVehicleAsync(this HttpClient client, VehicleInput input) =>
        client.PostAsJsonAsync(VehiclesUrl, input);

    public static async Task<VehicleDto> CreateVehicleAndReadAsync(this HttpClient client, VehicleInput input)
    {
        var response = await client.CreateVehicleAsync(input);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<VehicleDto>())!;
    }
}
