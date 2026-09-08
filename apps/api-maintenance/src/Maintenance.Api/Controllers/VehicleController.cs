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

    /// <summary>Replaces the vehicle's details with the same rules as registration.</summary>
    [HttpPut("{vehicleId:guid}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> Update(Guid vehicleId, VehicleInput input, CancellationToken cancellationToken) =>
        Ok(await vehicles.UpdateAsync(vehicleId, input, cancellationToken));

    /// <summary>Deletes the vehicle and, by cascade, its maintenance records.</summary>
    [HttpDelete("{vehicleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid vehicleId, CancellationToken cancellationToken)
    {
        await vehicles.DeleteAsync(vehicleId, cancellationToken);
        return NoContent();
    }
}
