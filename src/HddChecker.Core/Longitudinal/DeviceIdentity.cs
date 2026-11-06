using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Longitudinal;

public sealed record DeviceIdentity(string Kind, string Value)
{
    public string Key => $"{Kind}:{Value}";

    public static DeviceIdentity FromSnapshot(SmartDeviceSnapshot snapshot)
    {
        return GetCandidates(snapshot)[0];
    }

    public static IReadOnlyList<DeviceIdentity> GetCandidates(SmartDeviceSnapshot snapshot)
    {
        var candidates = new List<DeviceIdentity>(2);
        if (!string.IsNullOrWhiteSpace(snapshot.WorldWideName))
        {
            candidates.Add(new DeviceIdentity("wwn", Normalize(snapshot.WorldWideName)));
        }

        if (!string.IsNullOrWhiteSpace(snapshot.SerialNumber))
        {
            candidates.Add(new DeviceIdentity("serial", Normalize(snapshot.SerialNumber)));
        }

        if (candidates.Count == 0 &&
            !string.IsNullOrWhiteSpace(snapshot.ModelName) &&
            snapshot.CapacityBytes is not null)
        {
            candidates.Add(new DeviceIdentity(
                "model-capacity",
                $"{Normalize(snapshot.ModelName)}:{snapshot.CapacityBytes.Value}"));
        }

        return candidates.Count > 0
            ? candidates
            : throw new InvalidDataException(
                "La captura no contiene WWN, serie ni combinación de modelo y capacidad para identificar la unidad.");
    }

    public static bool ShareIdentifier(
        SmartDeviceSnapshot first,
        SmartDeviceSnapshot second) =>
        GetCandidates(first).Intersect(GetCandidates(second)).Any();

    public static DeviceIdentity SelectPreferred(IEnumerable<DeviceIdentity> identities) =>
        identities
            .Distinct()
            .OrderBy(identity => identity.Kind switch
            {
                "wwn" => 0,
                "serial" => 1,
                _ => 2
            })
            .ThenBy(identity => identity.Value)
            .First();

    private static string Normalize(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToUpperInvariant();
}
