using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.IntegrationTests.Auth;

public class VerifyEmailTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public VerifyEmailTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EmailedLink_VerifiesOnce_ThenIsRejected()
    {
        await _client.RegisterAsync("once@example.com");
        var token = await _client.LatestTokenAsync("once@example.com");

        var first = await _client.VerifyAsync(token);
        var second = await _client.VerifyAsync(token);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await second.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Should().ContainKey("token");
    }

    [Fact]
    public async Task ExpiredLink_IsRejected()
    {
        await _client.RegisterAsync("late@example.com");
        var token = await _client.LatestTokenAsync("late@example.com");
        _factory.Clock.Advance(TimeSpan.FromMinutes(31));

        var response = await _client.VerifyAsync(token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Resend_IssuesANewerLink_AndSupersedesTheOldOne()
    {
        await _client.RegisterAsync("again@example.com");
        var oldToken = await _client.LatestTokenAsync("again@example.com");

        var resend = await _client.ResendVerificationAsync("again@example.com");
        var newToken = await _client.LatestTokenAsync("again@example.com");
        var oldResult = await _client.VerifyAsync(oldToken);
        var newResult = await _client.VerifyAsync(newToken);

        resend.StatusCode.Should().Be(HttpStatusCode.Accepted);
        newToken.Should().NotBe(oldToken);
        oldResult.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        newResult.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Resend_ForUnknownEmail_Returns202WithoutSendingAnything()
    {
        var response = await _client.ResendVerificationAsync("nobody@example.com");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var emails = await _client.GetFromJsonAsync<List<Maintenance.Infrastructure.Email.RecordedEmail>>(AuthFlows.DevEmailsUrl);
        emails.Should().NotContain(e => e.To == "nobody@example.com");
    }

    [Fact]
    public async Task Verify_WithGarbageToken_Returns400()
    {
        var response = await _client.VerifyAsync("not-a-real-token");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
