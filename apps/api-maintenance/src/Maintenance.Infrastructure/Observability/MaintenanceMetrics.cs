using System.Diagnostics.Metrics;
using Maintenance.Application.Observability;

namespace Maintenance.Infrastructure.Observability;

/// <summary>
/// The only type that knows counter names. Built on the BCL meter so counters can be read
/// locally with dotnet-counters and exported later without code changes.
/// </summary>
public sealed class MaintenanceMetrics : IMaintenanceMetrics, IDisposable
{
    public const string MeterName = "Maintenance";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _requests;
    private readonly Counter<long> _signUps;
    private readonly Counter<long> _verifications;
    private readonly Counter<long> _logins;
    private readonly Counter<long> _failedLogins;
    private readonly Counter<long> _resetRequests;
    private readonly Counter<long> _resets;
    private readonly Counter<long> _emailsSent;
    private readonly Counter<long> _emailsFailed;

    public MaintenanceMetrics()
    {
        _requests = _meter.CreateCounter<long>("maintenance.requests", description: "HTTP requests completed, by status code");
        _signUps = _meter.CreateCounter<long>("maintenance.sign_ups", description: "Accounts created");
        _verifications = _meter.CreateCounter<long>("maintenance.verifications", description: "Email addresses verified");
        _logins = _meter.CreateCounter<long>("maintenance.logins", description: "Successful logins");
        _failedLogins = _meter.CreateCounter<long>("maintenance.failed_logins", description: "Rejected login attempts");
        _resetRequests = _meter.CreateCounter<long>("maintenance.reset_requests", description: "Password reset links issued");
        _resets = _meter.CreateCounter<long>("maintenance.resets", description: "Passwords reset");
        _emailsSent = _meter.CreateCounter<long>("maintenance.emails_sent", description: "Emails handed to the sender");
        _emailsFailed = _meter.CreateCounter<long>("maintenance.emails_failed", description: "Emails the sender rejected");
    }

    public void RequestCompleted(int statusCode) =>
        _requests.Add(1, new KeyValuePair<string, object?>("status", statusCode));

    public void SignUp() => _signUps.Add(1);

    public void Verification() => _verifications.Add(1);

    public void Login() => _logins.Add(1);

    public void FailedLogin() => _failedLogins.Add(1);

    public void ResetRequest() => _resetRequests.Add(1);

    public void Reset() => _resets.Add(1);

    public void EmailSent() => _emailsSent.Add(1);

    public void EmailFailed() => _emailsFailed.Add(1);

    public void Dispose() => _meter.Dispose();
}
