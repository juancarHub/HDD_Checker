using HddChecker.Core.Longitudinal;

namespace HddChecker.Core.Acquisition;

public sealed record CaptureCatalog(
    IReadOnlyList<DeviceCaptureHistory> Devices,
    IReadOnlyList<CaptureCatalogIssue> Issues)
{
    public static async Task<CaptureCatalog> LoadAsync(
        string capturesRoot,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(capturesRoot))
        {
            return new CaptureCatalog([], []);
        }

        var reader = new AcquisitionPackageReader();
        var entries = new List<CaptureCatalogEntry>();
        var issues = new List<CaptureCatalogIssue>();

        foreach (var metadataPath in Directory.EnumerateFiles(
                     capturesRoot,
                     "acquisition.json",
                     SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var package = await reader.ReadAsync(metadataPath, cancellationToken);
                var identity = DeviceIdentity.FromSnapshot(package.Snapshot);
                entries.Add(new CaptureCatalogEntry(
                    Path.GetDirectoryName(Path.GetFullPath(metadataPath))!,
                    identity,
                    package));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                issues.Add(new CaptureCatalogIssue(metadataPath, exception.Message));
            }
        }

        var devices = entries
            .GroupBy(entry => entry.Identity)
            .Select(group => new DeviceCaptureHistory(
                group.Key,
                group.OrderBy(entry => entry.Package.CapturedAtUtc).ToArray()))
            .OrderBy(device => device.Latest.Package.Snapshot.ModelName)
            .ThenBy(device => device.Identity.Key)
            .ToArray();

        return new CaptureCatalog(devices, issues);
    }
}
