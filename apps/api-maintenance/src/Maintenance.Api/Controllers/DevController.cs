using Maintenance.Api.Hosting;
using Maintenance.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.Api.Controllers;

/// <summary>
/// Development-only helpers. Outside the Development environment the controller has no routes
/// at all (see <see cref="DevelopmentOnlyConvention"/>), so these paths answer 404.
/// </summary>
[ApiController]
[Route("api/v1/dev")]
[AllowAnonymous]
[DevelopmentOnly]
public sealed class DevController(IRecordedEmails emails) : ControllerBase
{
    /// <summary>The most recent emails the sender recorded, newest first, so tests and the client can read links.</summary>
    [HttpGet("emails")]
    public ActionResult<IReadOnlyList<RecordedEmail>> Emails() => Ok(emails.Latest());
}
