using HddChecker.Core.Analysis;

namespace HddChecker.Storage;

public sealed record DeviceStatus(
    string IdentityKey,
    string? ModelName,
    string? Protocol,
    long CaptureCount,
    DateTimeOffset LatestCaptureUtc,
    bool? SmartPassed,
    string AcquisitionOutcome,
    long? PowerOnHours,
    int? TemperatureCelsius,
    int? RemainingLifePercent,
    int? NvmeAvailableSparePercent,
    long? ReallocatedSectors,
    long? PendingSectors,
    long? NvmeMediaErrors,
    FindingSeverity? HighestFindingSeverity,
    string? HighestFindingMessage);
