using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maintenance.Application.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Maintenance.IntegrationTests.Auth;

public class LoginTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public LoginTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_FreshAccount_ReturnsTokenAndUnverifiedFlag()
    {
        await _client.RegisterAsync("fresh@example.com");

        var response = await _client.LoginAsync("fresh@example.com");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResult>();
        result!.Token.Should().MatchRegex(@"^[\w-]+\.[\w-]+\.[\w-]+$");
        result.EmailVerified.Should().BeFalse();
        result.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_ProduceIdentical401Bodies()
    {
        await _client.RegisterAsync("known@example.com");

        var unknown = await _client.LoginAsync("unknown@example.com", "Whatever1");
        var wrong = await _client.LoginAsync("known@example.com", "Wrong999");

        unknown.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await unknown.Content.ReadAsStringAsync()).Should().Be(await wrong.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_BeyondTheRateLimit_Returns429()
    {
        using var limited = _factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimits:AuthPermitLimit"] = "3" })));
        using var client = limited.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            statuses.Add((await client.LoginAsync("anyone@example.com", "Whatever1")).StatusCode);
        }

        statuses.Take(3).Should().AllBeEquivalentTo(HttpStatusCode.Unauthorized);
        statuses[3].Should().Be(HttpStatusCode.TooManyRequests);
    }
}
