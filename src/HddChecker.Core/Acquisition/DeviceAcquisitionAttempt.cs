using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Acquisition;

public sealed record DeviceAcquisitionAttempt(
    SmartctlDevice Device,
    DeviceAcquisitionStatus Status,
    AcquisitionOutcome Outcome,
    string? CaptureDirectory,
    string? ErrorMessage,
    string? IdentityKey)
{
    public bool Succeeded => Status is DeviceAcquisitionStatus.Captured;
}
