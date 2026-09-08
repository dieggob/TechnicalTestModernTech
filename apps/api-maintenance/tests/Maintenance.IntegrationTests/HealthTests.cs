using System.Net;
using FluentAssertions;

namespace Maintenance.IntegrationTests;

public class HealthTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public HealthTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }
}
