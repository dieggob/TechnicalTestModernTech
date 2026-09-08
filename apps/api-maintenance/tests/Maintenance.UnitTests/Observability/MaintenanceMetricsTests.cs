using System.Diagnostics.Metrics;
using FluentAssertions;
using Maintenance.Infrastructure.Observability;

namespace Maintenance.UnitTests.Observability;

public class MaintenanceMetricsTests
{
    [Fact]
    public void RequestCompleted_IncrementsRequestsCounterWithStatusTag()
    {
        using var metrics = new MaintenanceMetrics();
        var observed = new List<(long Value, int Status)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MaintenanceMetrics.MeterName && instrument.Name == "maintenance.requests")
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            var status = (int)tags.ToArray().Single(tag => tag.Key == "status").Value!;
            observed.Add((value, status));
        });
        listener.Start();

        metrics.RequestCompleted(200);
        metrics.RequestCompleted(404);

        observed.Should().Equal((1, 200), (1, 404));
    }
}
