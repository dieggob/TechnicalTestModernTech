using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maintenance.Application.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Maintenance.IntegrationTests.Auth;

public class ProtectedEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ProtectedEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> WithVerificationRequired() => _factory.WithWebHostBuilder(builder =>
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Auth:RequireEmailVerification"] = "true" })));

    [Fact]
    public async Task WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync(TestEndpoints.Protected);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WithValidToken_Returns200AndTheCallersId()
    {
        var client = _factory.CreateClient();
        var login = await client.RegisterAndLoginAsync("caller@example.com");
        client.WithBearer(login.Token);

        var response = await client.GetAsync(TestEndpoints.Protected);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>();
        body!["userId"].Should().Be(login.UserId);
    }

    [Fact]
    public async Task FlagOff_UnverifiedAccount_IsAllowed()
    {
        var client = _factory.CreateClient();
        client.WithBearer(await client.LoginTokenAsync("unverified-ok@example.com"));

        (await client.GetAsync(TestEndpoints.Protected)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FlagOn_UnverifiedAccount_Gets403WithEmailNotVerifiedCode_UntilVerifiedMidSession()
    {
        using var gated = WithVerificationRequired();
        var client = gated.CreateClient();
        client.WithBearer(await client.LoginTokenAsync("gated@example.com"));

        var before = await client.GetAsync(TestEndpoints.Protected);
        await client.VerifyAsync(await client.LatestTokenAsync("gated@example.com"));
        var after = await client.GetAsync(TestEndpoints.Protected);

        before.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await before.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Extensions["code"]!.ToString().Should().Be("EmailNotVerified");
        after.StatusCode.Should().Be(HttpStatusCode.OK, "verification takes effect without a new login");
    }

    [Fact]
    public async Task FlagOn_AuthEndpointsStayOpen()
    {
        using var gated = WithVerificationRequired();
        var client = gated.CreateClient();

        var response = await client.RegisterAsync("still-open@example.com");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("http://localhost:4200", true)]
    [InlineData("http://evil.example", false)]
    public async Task Preflight_AllowsOnlyTheClientOrigin(string origin, bool allowed)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, TestEndpoints.Protected);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().Be(allowed);
    }
}

internal static class ProtectedEndpointFlows
{
    public static async Task<AuthResult> RegisterAndLoginAsync(this HttpClient client, string email)
    {
        await client.RegisterAsync(email);
        var response = await client.LoginAsync(email);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResult>())!;
    }
}
