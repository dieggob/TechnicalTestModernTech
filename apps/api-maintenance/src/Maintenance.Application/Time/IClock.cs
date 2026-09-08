namespace Maintenance.Application.Time;

/// <summary>The one seam for "now", so expiry rules are testable without waiting.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
