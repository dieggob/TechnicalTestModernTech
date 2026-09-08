namespace Maintenance.Application.Observability;

/// <summary>
/// Business and request counters. Methods are added by the slices that own the events,
/// so services record what happened without knowing how metrics are exported.
/// </summary>
public interface IMaintenanceMetrics
{
    void RequestCompleted(int statusCode);

    void SignUp();
}
