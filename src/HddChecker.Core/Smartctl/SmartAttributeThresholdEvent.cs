namespace HddChecker.Core.Smartctl;

public sealed record SmartAttributeThresholdEvent(
    int Id,
    string Name,
    int? Value,
    int? Worst,
    int? Threshold,
    string WhenFailed,
    string? RawValue);
