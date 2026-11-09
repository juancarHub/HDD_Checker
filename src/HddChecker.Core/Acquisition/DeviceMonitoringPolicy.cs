using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Acquisition;

public static class DeviceMonitoringPolicy
{
    private static readonly string[] ExternalTypePrefixes =
    [
        "usb",
        "snt",
        "sat",
    ];

    public static bool IsExternal(SmartctlDevice device)
    {
        var type = device.Type?.Trim();
        if (!string.IsNullOrWhiteSpace(type) &&
            ExternalTypePrefixes.Any(prefix =>
                type.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return device.InfoName?.Contains("USB", StringComparison.OrdinalIgnoreCase) is true;
    }
}
