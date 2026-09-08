using System.Net.Http.Json;
using Maintenance.Application.Maintenance;

namespace Maintenance.IntegrationTests.MaintenanceRecords;

/// <summary>Shared steps for maintenance-record journeys.</summary>
internal static class MaintenanceFlows
{
    public static string RecordsUrl(Guid vehicleId) => $"/api/v1/vehicles/{vehicleId}/maintenance";

    public static MaintenanceInput SampleRecord(int mileageAtService = 46000, string description = "Oil change") =>
        new(description, 89.99m, new DateOnly(2026, 9, 1), mileageAtService, "Quick Lube", "Synthetic 5W-30");

    public static Task<HttpResponseMessage> CreateRecordAsync(this HttpClient client, Guid vehicleId, MaintenanceInput input) =>
        client.PostAsJsonAsync(RecordsUrl(vehicleId), input);

    public static async Task<MaintenanceWriteResult> CreateRecordAndReadAsync(this HttpClient client, Guid vehicleId, MaintenanceInput input)
    {
        var response = await client.CreateRecordAsync(vehicleId, input);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MaintenanceWriteResult>())!;
    }
}
