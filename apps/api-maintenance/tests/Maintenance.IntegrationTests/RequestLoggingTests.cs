using FluentAssertions;
using Maintenance.Api.Observability;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Maintenance.IntegrationTests;

public class RequestLoggingTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RequestLoggingTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EveryRequest_ProducesOneStructuredEntryWithoutSecrets()
    {
        var logs = new CapturingLoggerProvider();
        using var client = _factory
            .WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddProvider(logs)))
            .CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "super-secret-token");

        await client.GetAsync("/health");

        var entries = logs.Entries.Where(entry => entry.Category == typeof(RequestLoggingMiddleware).FullName).ToList();
        entries.Should().ContainSingle()
            .Which.Message.Should().Contain("GET").And.Contain("/health").And.Contain("200").And.MatchRegex(@"in \d+ ms");
        entries.Single().Scope.Should().ContainKey("userId").WhoseValue.Should().BeNull();
        logs.Entries.Should().NotContain(entry => entry.Message.Contains("super-secret-token"));
    }
}
