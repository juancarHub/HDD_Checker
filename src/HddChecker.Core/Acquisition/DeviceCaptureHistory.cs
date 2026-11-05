using HddChecker.Core.Longitudinal;

namespace HddChecker.Core.Acquisition;

public sealed record DeviceCaptureHistory(
    DeviceIdentity Identity,
    IReadOnlyList<CaptureCatalogEntry> Captures)
{
    public CaptureCatalogEntry First => Captures[0];
    public CaptureCatalogEntry Latest => Captures[^1];
}
