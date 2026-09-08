using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.IntegrationTests.Auth;

public class ResetPasswordTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ResetPasswordTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EmailedLink_SetsANewPassword_OnceOnly()
    {
        const string email = "reset-journey@example.com";
        await _client.RegisterAsync(email, "OldSecret1");
        await _client.ForgotPasswordAsync(email);
        var token = await _client.LatestTokenAsync(email);

        var reset = await _client.ResetPasswordAsync(token, "NewSecret2");
        var newLogin = await _client.LoginAsync(email, "NewSecret2");
        var oldLogin = await _client.LoginAsync(email, "OldSecret1");
        var reuse = await _client.ResetPasswordAsync(token, "Another3");

        reset.StatusCode.Should().Be(HttpStatusCode.OK);
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        reuse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerificationLink_CannotResetAPassword()
    {
        const string email = "wrong-purpose@example.com";
        await _client.RegisterAsync(email);
        var verificationToken = await _client.LatestTokenAsync(email);

        var response = await _client.ResetPasswordAsync(verificationToken, "NewSecret2");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Should().ContainKey("token");
    }

    [Fact]
    public async Task WeakNewPassword_Returns400WithFieldError_AndKeepsTheLinkUsable()
    {
        const string email = "weak@example.com";
        await _client.RegisterAsync(email);
        await _client.ForgotPasswordAsync(email);
        var token = await _client.LatestTokenAsync(email);

        var weak = await _client.ResetPasswordAsync(token, "short");
        var strong = await _client.ResetPasswordAsync(token, "StrongEnough9");

        weak.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await weak.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Should().ContainKey("newPassword");
        strong.StatusCode.Should().Be(HttpStatusCode.OK, "input validation runs before the token is consumed");
    }

    [Fact]
    public async Task ExpiredLink_Returns400()
    {
        const string email = "late-reset@example.com";
        await _client.RegisterAsync(email);
        await _client.ForgotPasswordAsync(email);
        var token = await _client.LatestTokenAsync(email);
        _factory.Clock.Advance(TimeSpan.FromMinutes(31));

        var response = await _client.ResetPasswordAsync(token, "NewSecret2");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
