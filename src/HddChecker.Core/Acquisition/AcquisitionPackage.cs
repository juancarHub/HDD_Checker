using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Acquisition;

public sealed record AcquisitionPackage(
    int SchemaVersion,
    string CaptureId,
    DateTimeOffset CapturedAtUtc,
    AcquisitionCommandMetadata Command,
    SmartctlResultAssessment Assessment,
    SmartDeviceSnapshot Snapshot);
