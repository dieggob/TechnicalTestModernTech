using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.IntegrationTests.Auth;

public class RegisterTests : IClassFixture<ApiFactory>
{
    private const string Url = "/api/v1/auth/register";
    private readonly HttpClient _client;

    public RegisterTests(ApiFactory factory)
    {
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
