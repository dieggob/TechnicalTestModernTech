namespace Maintenance.Application.Exceptions;

/// <summary>
/// The requested resource does not exist for the caller. Deliberately also used when a
/// resource exists but belongs to another user, so ownership is never revealed (design decision).
/// </summary>
public sealed class NotFoundException(string resource) : Exception($"{resource} was not found.")
{
    public string Resource { get; } = resource;
}
