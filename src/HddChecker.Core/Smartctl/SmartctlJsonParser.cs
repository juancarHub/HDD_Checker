using System.Text.Json;

namespace HddChecker.Core.Smartctl;

public static class SmartctlJsonParser
{
    public static IReadOnlyList<string> ParseMessages(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("smartctl", out var smartctl) ||
            !smartctl.TryGetProperty("messages", out var messages) ||
            messages.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return messages.EnumerateArray()
            .Select(message => ReadString(message, "string"))
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Select(message => message!)
            .ToArray();
    }

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

    public static SmartDeviceSnapshot ParseSnapshot(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var attributes = ReadAtaAttributes(root);

        return new SmartDeviceSnapshot(
            ReadNestedString(root, "device", "name") ?? string.Empty,
            ReadNestedString(root, "device", "type"),
            ReadNestedString(root, "device", "protocol"),
            ReadString(root, "model_family"),
            ReadString(root, "model_name"),
            ReadString(root, "serial_number"),
            ReadWorldWideName(root),
            ReadNestedInt64(root, "user_capacity", "bytes"),
            ReadNestedBoolean(root, "smart_status", "passed"),
            ReadNestedInt64(root, "power_on_time", "hours"),
            ReadInt64(root, "power_cycle_count"),
            ReadNestedInt32(root, "temperature", "current"),
            ReadRemainingLife(root, attributes),
            ReadRawAttribute(attributes, 5),
            ReadRawAttribute(attributes, 197),
            ReadRawAttribute(attributes, 198),
            ReadRawAttribute(attributes, 187),
            ReadRawAttribute(attributes, 199));
    }

    private static Dictionary<int, JsonElement> ReadAtaAttributes(JsonElement root)
    {
        if (!root.TryGetProperty("ata_smart_attributes", out var smartAttributes) ||
            !smartAttributes.TryGetProperty("table", out var table) ||
            table.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return table.EnumerateArray()
            .Where(item => item.TryGetProperty("id", out var id) && id.TryGetInt32(out _))
            .ToDictionary(item => item.GetProperty("id").GetInt32());
    }

    private static long? ReadRawAttribute(IReadOnlyDictionary<int, JsonElement> attributes, int id)
    {
        if (!attributes.TryGetValue(id, out var attribute) ||
            !attribute.TryGetProperty("raw", out var raw) ||
            !raw.TryGetProperty("value", out var value) ||
            !value.TryGetInt64(out var number))
        {
            return null;
        }

        return number;
    }

    private static int? ReadRemainingLife(
        JsonElement root,
        IReadOnlyDictionary<int, JsonElement> attributes)
    {
        if (root.TryGetProperty("endurance_used", out var endurance) &&
            endurance.TryGetProperty("current_percent", out var used) &&
            used.TryGetInt32(out var usedPercent))
        {
            return Math.Clamp(100 - usedPercent, 0, 100);
        }

        foreach (var id in new[] { 231, 202 })
        {
            if (attributes.TryGetValue(id, out var attribute) &&
                attribute.TryGetProperty("value", out var value) &&
                value.TryGetInt32(out var remaining))
            {
                return Math.Clamp(remaining, 0, 100);
            }
        }

        return null;
    }

    private static string? ReadWorldWideName(JsonElement root)
    {
        if (!root.TryGetProperty("wwn", out var wwn))
        {
            return null;
        }

        if (wwn.ValueKind == JsonValueKind.String)
        {
            return wwn.GetString();
        }

        if (wwn.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var naa = ReadInt64(wwn, "naa");
        var oui = ReadInt64(wwn, "oui");
        var id = ReadInt64(wwn, "id");
        return naa is null || oui is null || id is null
            ? null
            : $"{naa:x}-{oui:x6}-{id:x}";
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static string? ReadNestedString(
        JsonElement element,
        string parentName,
        string propertyName) =>
        element.TryGetProperty(parentName, out var parent)
            ? ReadString(parent, propertyName)
            : null;

    private static long? ReadInt64(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetInt64(out var value)
            ? value
            : null;

    private static long? ReadNestedInt64(
        JsonElement element,
        string parentName,
        string propertyName) =>
        element.TryGetProperty(parentName, out var parent)
            ? ReadInt64(parent, propertyName)
            : null;

    private static int? ReadNestedInt32(
        JsonElement element,
        string parentName,
        string propertyName) =>
        element.TryGetProperty(parentName, out var parent) &&
        parent.TryGetProperty(propertyName, out var property) &&
        property.TryGetInt32(out var value)
            ? value
            : null;

    private static bool? ReadNestedBoolean(
        JsonElement element,
        string parentName,
        string propertyName)
    {
        if (!element.TryGetProperty(parentName, out var parent) ||
            !parent.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }
}
