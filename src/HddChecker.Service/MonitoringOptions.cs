namespace HddChecker.Service;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public string? SmartctlPath { get; init; }

    public int TimeoutSeconds { get; init; } = 30;

    public int IntervalMinutes { get; init; } = 360;

    public string? DataRoot { get; init; }
}
