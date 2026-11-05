using System.Text.Json;

namespace HddChecker.Core.Smartctl;

public static class SmartctlJsonParser
{
    public static IReadOnlyList<SmartctlDevice> ParseScan(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("devices", out var devices) ||
            devices.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return devices.EnumerateArray()
            .Select(device => new SmartctlDevice(
                ReadString(device, "name") ?? string.Empty,
                ReadString(device, "type"),
                ReadString(device, "protocol")))
            .Where(device => !string.IsNullOrWhiteSpace(device.Name))
            .ToArray();
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
