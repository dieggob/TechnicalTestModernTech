using Maintenance.Application.Time;

namespace Maintenance.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
