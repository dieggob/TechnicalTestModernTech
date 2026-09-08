using FluentValidation;

namespace Maintenance.Application.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public const int MaximumEmailLength = 320;

    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(MaximumEmailLength)
            .EmailAddress().WithMessage("Email is not a valid address.");

        RuleFor(request => request.Password).Password();
    }
}
