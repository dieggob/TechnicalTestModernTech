using System.Diagnostics;
using System.Security.Claims;
using Maintenance.Application.Observability;

namespace Maintenance.Api.Observability;

/// <summary>
/// Writes one structured log entry per request with method, path, status, and latency,
/// inside a scope carrying the caller's user id when authenticated. Headers and bodies are
/// never logged, so tokens and passwords cannot leak through this path.
/// </summary>
public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IMaintenanceMetrics metrics)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            using (logger.BeginScope(new Dictionary<string, object?> { ["userId"] = userId }))
            {
                logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                    context.Request.Method, context.Request.Path.Value, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
            }

            metrics.RequestCompleted(context.Response.StatusCode);
        }
    }
}
