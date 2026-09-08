using FluentValidation;

namespace Maintenance.Application.Auth;

public sealed record VerifyEmailRequest(string Token);

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(request => request.Token).NotEmpty().WithMessage("Token is required.");
    }
}
