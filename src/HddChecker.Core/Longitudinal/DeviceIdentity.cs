using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Longitudinal;

public sealed record DeviceIdentity(string Kind, string Value)
{
    public string Key => $"{Kind}:{Value}";

    public static DeviceIdentity FromSnapshot(SmartDeviceSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(snapshot.WorldWideName))
        {
            return new DeviceIdentity("wwn", Normalize(snapshot.WorldWideName));
        }

        if (!string.IsNullOrWhiteSpace(snapshot.SerialNumber))
        {
            return new DeviceIdentity("serial", Normalize(snapshot.SerialNumber));
        }

        if (!string.IsNullOrWhiteSpace(snapshot.ModelName) && snapshot.CapacityBytes is not null)
        {
            return new DeviceIdentity(
                "model-capacity",
                $"{Normalize(snapshot.ModelName)}:{snapshot.CapacityBytes.Value}");
        }

        throw new InvalidDataException(
            "La captura no contiene WWN, serie ni combinación de modelo y capacidad para identificar la unidad.");
    }

    private static string Normalize(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToUpperInvariant();
}
