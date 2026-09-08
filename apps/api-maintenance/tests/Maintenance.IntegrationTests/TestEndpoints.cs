using Maintenance.Application.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Data.Sqlite;

namespace Maintenance.IntegrationTests;

/// <summary>
/// Endpoints that exist only in the test host, so cross-cutting behaviour (error mapping,
/// authorization) can be exercised without depending on a feature endpoint.
/// Added to the application's endpoint data sources by <see cref="StartupFilter"/>, never in Program.cs.
/// </summary>
internal static class TestEndpoints
{
    public const string NotFound = "/__test/not-found";
    public const string Conflict = "/__test/conflict";
    public const string Validation = "/__test/validation";
    public const string DatabaseUnavailable = "/__test/db-unavailable";
    public const string Boom = "/__test/boom";

    public static EndpointDataSource DataSource() => new DefaultEndpointDataSource(
        Endpoint(NotFound, _ => throw new NotFoundException("Vehicle")),
        Endpoint(Conflict, _ => throw new ConflictException("VIN already registered.")),
        Endpoint(Validation, _ => throw new ValidationException(new Dictionary<string, string[]>
        {
            ["email"] = ["Email is required."],
            ["password"] = ["Password is too short.", "Password needs a digit."],
        })),
        Endpoint(DatabaseUnavailable, _ => throw new SqliteException("unable to open database file", 14)),
        Endpoint(Boom, _ => throw new InvalidOperationException("secret detail that must not leak")));

    private static RouteEndpoint Endpoint(string path, RequestDelegate handler) =>
        new(handler, RoutePatternFactory.Parse(path), order: 0, metadata: null, displayName: path);

    /// <summary>
    /// Runs after the application has configured its pipeline (so routing exists) and before
    /// the pipeline is built (so the routing middleware still picks the sources up), and appends
    /// the test endpoints to the application's own endpoint data sources.
    /// </summary>
    public sealed class StartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            var routes = (IEndpointRouteBuilder)app.Properties["__EndpointRouteBuilder"]!;
            routes.DataSources.Add(DataSource());
        };
    }
}
