using HddChecker.Core.Longitudinal;

namespace HddChecker.Core.Acquisition;

public sealed record CaptureCatalogEntry(
    string DirectoryPath,
    DeviceIdentity Identity,
    AcquisitionPackage Package);
