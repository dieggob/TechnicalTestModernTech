using Maintenance.Application.Vehicles;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.Api.Controllers;

/// <summary>Vehicles of the calling user. Every action requires a session (fallback policy).</summary>
[ApiController]
[Route("api/v1/vehicles")]
[Produces("application/json")]
public sealed class VehicleController(VehicleService vehicles) : ControllerBase
{
    /// <summary>Registers a vehicle. The VIN must be unique among the caller's vehicles.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> Create(VehicleInput input, CancellationToken cancellationToken)
    {
        var vehicle = await vehicles.CreateAsync(input, cancellationToken);
        return Created($"/api/v1/vehicles/{vehicle.Id}", vehicle);
    }
}
