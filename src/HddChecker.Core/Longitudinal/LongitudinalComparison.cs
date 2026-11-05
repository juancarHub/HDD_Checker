namespace HddChecker.Core.Longitudinal;

public sealed record LongitudinalComparison(
    DeviceIdentity Identity,
    DateTimeOffset PreviousCaptureUtc,
    DateTimeOffset CurrentCaptureUtc,
    TimeSpan CalendarElapsed,
    IReadOnlyList<MetricChange> Metrics,
    IReadOnlyList<MetricRate> Rates)
{
    public long? PowerOnHoursElapsed =>
        Metrics.FirstOrDefault(metric => metric.Name == "Horas de funcionamiento")?.Delta;
}
