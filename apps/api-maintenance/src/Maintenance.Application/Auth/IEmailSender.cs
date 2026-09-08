namespace Maintenance.Application.Auth;

/// <summary>
/// Outbound email. The only implementation in this local-only project writes to the log and
/// records messages so tests and the client can read the links.
/// </summary>
public interface IEmailSender
{
    Task SendVerificationAsync(string email, string link, CancellationToken cancellationToken);

    Task SendPasswordResetAsync(string email, string link, CancellationToken cancellationToken);
}
