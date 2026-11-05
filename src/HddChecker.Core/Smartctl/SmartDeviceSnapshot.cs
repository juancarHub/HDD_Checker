namespace HddChecker.Core.Smartctl;

public sealed record SmartDeviceSnapshot(
    string DeviceName,
    string? DeviceType,
    string? Protocol,
    string? ModelFamily,
    string? ModelName,
    string? SerialNumber,
    string? WorldWideName,
    long? CapacityBytes,
    bool? SmartPassed,
    long? PowerOnHours,
    long? PowerCycleCount,
    int? TemperatureCelsius,
    int? RemainingLifePercent,
    long? ReallocatedSectors,
    long? PendingSectors,
    long? OfflineUncorrectableSectors,
    long? ReportedUncorrectableErrors,
    long? SataCrcErrors);
