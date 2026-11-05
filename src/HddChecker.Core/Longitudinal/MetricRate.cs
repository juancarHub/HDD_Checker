namespace HddChecker.Core.Longitudinal;

public sealed record MetricRate(
    string MetricName,
    long Delta,
    double? PerPowerOnHour,
    double? PerCalendarDay,
    string Unit);
