using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maintenance.Infrastructure.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.IntegrationTests.Auth;

public class RegisterTests : IClassFixture<ApiFactory>
{
    private const string Url = "/api/v1/auth/register";
    private const string DevEmailsUrl = "/api/v1/dev/emails";
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public RegisterTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_NewEmail_Returns201()
    {
        var response = await _client.PostAsJsonAsync(Url, new { email = "new@example.com", password = "Secret123" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body.Should().ContainKey("message");
    }

    [Fact]
    public async Task Register_RecordsAVerificationEmailWithALinkToTheClient()
    {
        await _client.PostAsJsonAsync(Url, new { email = "verify-me@example.com", password = "Secret123" });

        var emails = await _client.GetFromJsonAsync<List<RecordedEmail>>(DevEmailsUrl);

        var email = emails!.Should().ContainSingle(e => e.To == "verify-me@example.com").Subject;
        email.Subject.Should().Contain("Verify");
        email.Link.Should().StartWith("http://localhost:4200/verify?token=").And.MatchRegex("token=[A-Za-z0-9_-]{40,}$");
    }

    [Fact]
    public async Task DevEmails_OutsideDevelopment_DoNotExist()
    {
        using var production = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = production.CreateClient();

        var response = await client.GetAsync(DevEmailsUrl);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        await _client.PostAsJsonAsync(Url, new { email = "dup@example.com", password = "Secret123" });

        var response = await _client.PostAsJsonAsync(Url, new { email = "DUP@example.com", password = "Other456" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Title.Should().Be("Email is already registered.");
    }

    [Fact]
    public async Task Register_InvalidInput_Returns400WithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync(Url, new { email = "nope", password = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKeys("email", "password");
    }
}
