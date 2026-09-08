using Microsoft.AspNetCore.Mvc.Testing;

namespace Maintenance.IntegrationTests;

/// <summary>
/// Boots the real API pipeline in-process for integration tests.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
}
