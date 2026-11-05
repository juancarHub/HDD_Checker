using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Acquisition;

public sealed record DeviceAcquisitionAttempt(
    SmartctlDevice Device,
    AcquisitionOutcome Outcome,
    string? CaptureDirectory,
    string? ErrorMessage)
{
    public bool Succeeded => CaptureDirectory is not null;
}
