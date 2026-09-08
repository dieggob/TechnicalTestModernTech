using Maintenance.Application.Time;

namespace Maintenance.IntegrationTests;

/// <summary>A clock tests can move, so expiry rules run without waiting.</summary>
public sealed class TestClock : IClock
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;

    public void Advance(TimeSpan by) => UtcNow += by;
}
