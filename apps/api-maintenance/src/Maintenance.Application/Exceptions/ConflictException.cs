namespace Maintenance.Application.Exceptions;

/// <summary>
/// The request conflicts with existing state, such as a duplicate email or VIN.
/// </summary>
public sealed class ConflictException(string message) : Exception(message);
