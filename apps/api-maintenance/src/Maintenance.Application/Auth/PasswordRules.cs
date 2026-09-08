using FluentValidation;

namespace Maintenance.Application.Auth;

/// <summary>
/// The one definition of an acceptable password, shared by registration and password reset.
/// </summary>
public static class PasswordRules
{
    public const int MinimumLength = 8;

    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) => rule
        .NotEmpty().WithMessage("Password is required.")
        .MinimumLength(MinimumLength).WithMessage($"Password must be at least {MinimumLength} characters.")
        .Matches("[A-Za-z]").WithMessage("Password must contain a letter.")
        .Matches("[0-9]").WithMessage("Password must contain a digit.");
}
