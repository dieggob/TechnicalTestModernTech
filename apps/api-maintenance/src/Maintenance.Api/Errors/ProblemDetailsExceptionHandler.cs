using System.Data.Common;
using Maintenance.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Maintenance.Api.Errors;

/// <summary>
/// The one place that knows which HTTP status each application exception maps to.
/// Services throw typed exceptions; this handler turns them into ProblemDetails responses.
/// </summary>
public sealed class ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            NotFoundException notFound => Problem(StatusCodes.Status404NotFound, notFound.Message),
            ConflictException conflict => Problem(StatusCodes.Status409Conflict, conflict.Message),
            UnauthorizedException unauthorized => Problem(StatusCodes.Status401Unauthorized, unauthorized.Message),
            ValidationException validation => new ValidationProblemDetails(
                validation.Errors.ToDictionary(error => error.Key, error => error.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = validation.Message,
            },
            DbException => Problem(StatusCodes.Status503ServiceUnavailable, "The database is unavailable."),
            _ => Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception mapped to {Status}", problem.Status);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        // Serialize with the runtime type so ValidationProblemDetails keeps its "errors" member.
        await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), cancellationToken);
        return true;
    }

    private static ProblemDetails Problem(int status, string title) => new() { Status = status, Title = title };
}
