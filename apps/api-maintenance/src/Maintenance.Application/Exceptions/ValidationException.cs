namespace Maintenance.Application.Exceptions;

/// <summary>
/// The input failed validation. <see cref="Errors"/> maps each field (camelCase, as the
/// client sees it) to its messages.
/// </summary>
public sealed class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
