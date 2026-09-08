using FluentValidation;
using Maintenance.Application.Exceptions;
using Maintenance.Application.Observability;
using Maintenance.Application.Validation;
using Maintenance.Domain.Users;

namespace Maintenance.Application.Auth;

/// <summary>
/// Account use cases. Each public method is one behaviour from the design's auth flows.
/// </summary>
public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IValidator<RegisterRequest> registerValidator,
    IMaintenanceMetrics metrics)
{
    public async Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        ValidationRunner.Validate(registerValidator, request);

        if (await users.FindByEmailAsync(request.Email, cancellationToken) is not null)
        {
            throw new ConflictException("Email is already registered.");
        }

        var user = User.Create(request.Email, passwordHasher.Hash(request.Password), DateTime.UtcNow);
        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        metrics.SignUp();
    }
}
