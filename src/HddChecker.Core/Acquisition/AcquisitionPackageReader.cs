using System.Text.Json;
using System.Text.Json.Serialization;
using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Acquisition;

public sealed class AcquisitionPackageReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<AcquisitionPackage> ReadAsync(
        string capturePath,
        CancellationToken cancellationToken = default)
    {
        var metadataPath = Directory.Exists(capturePath)
            ? Path.Combine(capturePath, "acquisition.json")
            : capturePath;

        AcquisitionPackage package;
        await using (var stream = File.OpenRead(metadataPath))
        {
            package = await JsonSerializer.DeserializeAsync<AcquisitionPackage>(
                          stream,
                          JsonOptions,
                          cancellationToken)
                      ?? throw new InvalidDataException("El paquete de adquisición está vacío.");
        }

        var captureDirectory = Path.GetDirectoryName(Path.GetFullPath(metadataPath))!;
        var rawJsonPath = Path.Combine(captureDirectory, "smartctl.json");
        if (!File.Exists(rawJsonPath))
        {
            return package;
        }

        var rawJson = await File.ReadAllTextAsync(rawJsonPath, cancellationToken);
        return package with { Snapshot = SmartctlJsonParser.ParseSnapshot(rawJson) };
    }
}
