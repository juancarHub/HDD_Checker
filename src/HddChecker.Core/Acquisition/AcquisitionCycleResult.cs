namespace HddChecker.Core.Acquisition;

public sealed record AcquisitionCycleResult(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<DeviceAcquisitionAttempt> Attempts)
{
    public int DiscoveredCount => Attempts.Count;
    public int CapturedCount => Attempts.Count(attempt => attempt.Succeeded);
    public int FailedCount => Attempts.Count(attempt => !attempt.Succeeded);
}
