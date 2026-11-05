namespace HddChecker.Service;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public string SmartctlPath { get; init; } =
        @"C:\Program Files\smartmontools\bin\smartctl.exe";

    public int TimeoutSeconds { get; init; } = 30;

    public int IntervalMinutes { get; init; } = 360;

    public string? DataRoot { get; init; }
}
