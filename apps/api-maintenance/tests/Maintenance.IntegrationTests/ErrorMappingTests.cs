using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.IntegrationTests;

public class ErrorMappingTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ErrorMappingTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData(TestEndpoints.NotFound, HttpStatusCode.NotFound, "Vehicle was not found.")]
    [InlineData(TestEndpoints.Conflict, HttpStatusCode.Conflict, "VIN already registered.")]
    [InlineData(TestEndpoints.DatabaseUnavailable, HttpStatusCode.ServiceUnavailable, "The database is unavailable.")]
    [InlineData(TestEndpoints.Boom, HttpStatusCode.InternalServerError, "An unexpected error occurred.")]
    public async Task ApplicationExceptions_MapToProblemDetails(string path, HttpStatusCode status, string title)
    {
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(status);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be((int)status);
        problem.Title.Should().Be(title);
    }

    [Fact]
    public async Task ValidationException_MapsTo400WithFieldErrors()
    {
        var response = await _client.GetAsync(TestEndpoints.Validation);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("email").WhoseValue.Should().ContainSingle();
        problem.Errors.Should().ContainKey("password").WhoseValue.Should().HaveCount(2);
    }

    [Fact]
    public async Task UnexpectedException_DoesNotLeakDetails()
    {
        var response = await _client.GetAsync(TestEndpoints.Boom);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("secret detail").And.NotContain("InvalidOperationException");
    }
}
