using Maintenance.Application.Auth;
using Microsoft.Extensions.Logging;

namespace Maintenance.Infrastructure.Email;

/// <summary>A message the sender handed off, kept so tests and the Development endpoint can read the link.</summary>
public sealed record RecordedEmail(string To, string Subject, string Link, DateTime SentAt);

/// <summary>Read side of the recorded messages.</summary>
public interface IRecordedEmails
{
    IReadOnlyList<RecordedEmail> Latest();
}

/// <summary>
/// The only email sender in this local-only project: writes each message to the log and keeps
/// the most recent ones in memory. Nothing is written to disk or sent over the network.
/// </summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender, IRecordedEmails
{
    public const int Capacity = 50;

    private readonly object _gate = new();
    private readonly Queue<RecordedEmail> _recent = new();

    public Task SendVerificationAsync(string email, string link, CancellationToken cancellationToken) =>
        RecordAsync(email, "Verify your email address", link);

    public Task SendPasswordResetAsync(string email, string link, CancellationToken cancellationToken) =>
        RecordAsync(email, "Reset your password", link);

    public IReadOnlyList<RecordedEmail> Latest()
    {
        lock (_gate)
        {
            return _recent.Reverse().ToList();
        }
    }

    private Task RecordAsync(string email, string subject, string link)
    {
        var message = new RecordedEmail(email, subject, link, DateTime.UtcNow);
        lock (_gate)
        {
            _recent.Enqueue(message);
            while (_recent.Count > Capacity)
            {
                _recent.Dequeue();
            }
        }

        logger.LogInformation("Email to {To}: {Subject} {Link}", email, subject, link);
        return Task.CompletedTask;
    }
}
