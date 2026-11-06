using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Analysis;

public sealed record DeviceHealthSignals(
    bool? SmartPassed,
    AcquisitionOutcome AcquisitionOutcome,
    string? Protocol,
    long CaptureCount,
    int? TemperatureCelsius,
    int? RemainingLifePercent,
    int? NvmeAvailableSparePercent,
    long? ReallocatedSectors,
    long? PendingSectors,
    long? NvmeMediaErrors,
    FindingSeverity? HighestFindingSeverity,
    string? HighestFindingMessage);
