namespace Maintenance.Application.Exceptions;

/// <summary>
/// Credentials were not accepted. One message for unknown email and wrong password alike,
/// so the endpoint never reveals which emails are registered (design decision).
/// </summary>
public sealed class UnauthorizedException() : Exception("Invalid email or password.");
