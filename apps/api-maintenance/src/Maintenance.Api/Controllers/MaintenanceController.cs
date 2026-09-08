using Maintenance.Application.Maintenance;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.Api.Controllers;

/// <summary>Maintenance records of one of the caller's vehicles. Every action requires a session (fallback policy).</summary>
[ApiController]
[Route("api/v1/vehicles/{vehicleId:guid}/maintenance")]
[Produces("application/json")]
public sealed class MaintenanceController(MaintenanceService maintenance) : ControllerBase
{
    /// <summary>Logs a job. When its mileage exceeds the vehicle's, the vehicle's current mileage is raised in the same transaction.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MaintenanceWriteResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceWriteResult>> Create(Guid vehicleId, MaintenanceInput input, CancellationToken cancellationToken)
    {
        var result = await maintenance.CreateAsync(vehicleId, input, cancellationToken);
        return Created($"/api/v1/vehicles/{vehicleId}/maintenance/{result.Record.Id}", result);
    }
}
