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
        var loaded = new List<(string DirectoryPath, AcquisitionPackage Package, IReadOnlyList<DeviceIdentity> Identities)>();
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
                var identities = DeviceIdentity.GetCandidates(package.Snapshot);
                loaded.Add((
                    Path.GetDirectoryName(Path.GetFullPath(metadataPath))!,
                    package,
                    identities));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                issues.Add(new CaptureCatalogIssue(metadataPath, exception.Message));
            }
        }

        var parent = new Dictionary<DeviceIdentity, DeviceIdentity>();
        foreach (var item in loaded)
        {
            foreach (var identity in item.Identities)
            {
                parent.TryAdd(identity, identity);
            }

            for (var index = 1; index < item.Identities.Count; index++)
            {
                Union(parent, item.Identities[0], item.Identities[index]);
            }
        }

        var preferredByRoot = parent.Keys
            .GroupBy(identity => Find(parent, identity))
            .ToDictionary(
                group => group.Key,
                group => DeviceIdentity.SelectPreferred(group));
        var entries = loaded.Select(item =>
        {
            var root = Find(parent, item.Identities[0]);
            return new CaptureCatalogEntry(
                item.DirectoryPath,
                preferredByRoot[root],
                item.Package);
        });

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

    private static DeviceIdentity Find(
        IDictionary<DeviceIdentity, DeviceIdentity> parent,
        DeviceIdentity identity)
    {
        if (parent[identity] != identity)
        {
            parent[identity] = Find(parent, parent[identity]);
        }

        return parent[identity];
    }

    private static void Union(
        IDictionary<DeviceIdentity, DeviceIdentity> parent,
        DeviceIdentity first,
        DeviceIdentity second)
    {
        var firstRoot = Find(parent, first);
        var secondRoot = Find(parent, second);
        if (firstRoot != secondRoot)
        {
            parent[secondRoot] = firstRoot;
        }
    }
}
