using System.Text.Json;
using System.Text.Json.Serialization;
using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Acquisition;

public sealed class AcquisitionPackageWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<string> WriteAsync(
        string capturesRoot,
        SmartctlCommandResult command,
        SmartctlResultAssessment assessment,
        SmartDeviceSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capturesRoot);

        var captureId = CreateCaptureId(command.StartedAtUtc, snapshot.DeviceName);
        var captureDirectory = CreateUniqueDirectory(capturesRoot, captureId);
        var package = new AcquisitionPackage(
            1,
            Path.GetFileName(captureDirectory),
            command.StartedAtUtc,
            AcquisitionCommandMetadata.FromResult(command),
            assessment,
            snapshot);

        await WriteTextAsync(
            Path.Combine(captureDirectory, "smartctl.json"),
            command.StandardOutput,
            cancellationToken);
        await WriteJsonAsync(
            Path.Combine(captureDirectory, "acquisition.json"),
            package,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(command.StandardError))
        {
            await WriteTextAsync(
                Path.Combine(captureDirectory, "smartctl.stderr.txt"),
                command.StandardError,
                cancellationToken);
        }

        return captureDirectory;
    }

    private static string CreateCaptureId(DateTimeOffset timestamp, string deviceName)
    {
        var safeDeviceName = string.Concat(deviceName
            .Replace("/dev/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Select(character => char.IsLetterOrDigit(character) ? character : '-'));

        if (string.IsNullOrWhiteSpace(safeDeviceName))
        {
            safeDeviceName = "device";
        }

        return $"{timestamp:yyyyMMddTHHmmss.fffffffZ}_{safeDeviceName}";
    }

    private static string CreateUniqueDirectory(string capturesRoot, string captureId)
    {
        Directory.CreateDirectory(capturesRoot);

        for (var suffix = 0; ; suffix++)
        {
            var directoryName = suffix == 0 ? captureId : $"{captureId}_{suffix}";
            var candidate = Path.Combine(capturesRoot, directoryName);
            if (Directory.Exists(candidate))
            {
                continue;
            }

            Directory.CreateDirectory(candidate);
            return Path.GetFullPath(candidate);
        }
    }

    private static async Task WriteJsonAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
        await stream.WriteAsync("\n"u8.ToArray(), cancellationToken);
    }

    private static Task WriteTextAsync(
        string path,
        string value,
        CancellationToken cancellationToken) =>
        File.WriteAllTextAsync(path, value, cancellationToken);
}
