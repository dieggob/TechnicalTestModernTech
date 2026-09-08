using FluentValidation;

namespace Maintenance.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid address.");
        RuleFor(request => request.Password).NotEmpty().WithMessage("Password is required.");
    }
}
