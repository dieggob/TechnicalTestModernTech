using FluentValidation;

namespace Maintenance.Application.Auth;

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(request => request.Token).NotEmpty().WithMessage("Token is required.");
        RuleFor(request => request.NewPassword).Password();
    }
}
