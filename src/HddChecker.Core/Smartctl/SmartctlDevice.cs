namespace HddChecker.Core.Smartctl;

public sealed record SmartctlDevice(string Name, string? Type, string? Protocol);
