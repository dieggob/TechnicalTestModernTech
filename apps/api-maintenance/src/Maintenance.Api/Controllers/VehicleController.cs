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
        return CreatedAtAction(nameof(Get), new { vehicleId = vehicle.Id }, vehicle);
    }

    /// <summary>The caller's vehicles.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<VehicleDto>>> List(CancellationToken cancellationToken) =>
        Ok(await vehicles.ListAsync(cancellationToken));

    /// <summary>One of the caller's vehicles. Others' vehicles are indistinguishable from missing ones.</summary>
    [HttpGet("{vehicleId:guid}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleDto>> Get(Guid vehicleId, CancellationToken cancellationToken) =>
        Ok(await vehicles.GetAsync(vehicleId, cancellationToken));
}
