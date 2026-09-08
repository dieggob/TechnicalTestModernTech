using FluentValidation;

namespace Maintenance.Application.Auth;

public sealed record ResendVerificationRequest(string Email);

public sealed class ResendVerificationRequestValidator : AbstractValidator<ResendVerificationRequest>
{
    public ResendVerificationRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid address.");
    }
}
