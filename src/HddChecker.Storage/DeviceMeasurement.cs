namespace HddChecker.Storage;

public sealed record DeviceMeasurement(
    string CaptureId,
    DateTimeOffset CapturedAtUtc,
    string AcquisitionOutcome,
    bool? SmartPassed,
    long? PowerOnHours,
    int? TemperatureCelsius,
    int? RemainingLifePercent,
    long? ReallocatedSectors,
    long? PendingSectors,
    long? NvmeMediaErrors);
