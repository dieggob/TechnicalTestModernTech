using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maintenance.Infrastructure.Email;

namespace Maintenance.IntegrationTests.Auth;

public class ForgotPasswordTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ForgotPasswordTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ForgotPassword_KnownAndUnknownEmails_GetIdentical202Bodies()
    {
        await _client.RegisterAsync("forgetful@example.com");

        var known = await _client.ForgotPasswordAsync("forgetful@example.com");
        var unknown = await _client.ForgotPasswordAsync("stranger@example.com");

        known.StatusCode.Should().Be(HttpStatusCode.Accepted);
        unknown.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await known.Content.ReadAsStringAsync()).Should().Be(await unknown.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ForgotPassword_RecordsAResetEmailOnlyForTheKnownAddress()
    {
        await _client.RegisterAsync("resetme@example.com");

        await _client.ForgotPasswordAsync("resetme@example.com");
        await _client.ForgotPasswordAsync("ghost@example.com");

        var emails = await _client.GetFromJsonAsync<List<RecordedEmail>>(AuthFlows.DevEmailsUrl);
        emails!.Where(e => e.To == "resetme@example.com" && e.Subject.Contains("Reset")).Should().ContainSingle()
            .Which.Link.Should().StartWith("http://localhost:4200/reset-password?token=");
        emails.Should().NotContain(e => e.To == "ghost@example.com");
    }

    [Fact]
    public async Task ForgotPassword_MalformedEmail_Returns400()
    {
        var response = await _client.ForgotPasswordAsync("not-an-email");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
