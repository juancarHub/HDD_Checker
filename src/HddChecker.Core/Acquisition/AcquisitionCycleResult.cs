using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Acquisition;

public sealed record AcquisitionCycleResult(
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<DeviceAcquisitionAttempt> Attempts,
    IReadOnlyList<SmartctlDevice> IgnoredExternalDevices)
{
    public int DiscoveredCount => Attempts.Count + IgnoredExternalDevices.Count;
    public int CapturedCount => Attempts.Count(attempt => attempt.Succeeded);
    public int FailedCount => Attempts.Count(attempt => attempt.Status is DeviceAcquisitionStatus.Failed);
    public int DuplicateRouteCount => Attempts.Count(attempt => attempt.Status is DeviceAcquisitionStatus.DuplicateRoute);
    public int IgnoredExternalCount => IgnoredExternalDevices.Count;
}
