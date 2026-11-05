using HddChecker.Core.Acquisition;
using HddChecker.Core.Longitudinal;
using HddChecker.Core.Smartctl;
using HddChecker.Storage;

namespace HddChecker.Core.Tests;

public sealed class CaptureIndexTests
{
    [Fact]
    public async Task SynchronizationIsIdempotent()
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"hdd-checker-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            var package = CreatePackage();
            var identity = DeviceIdentity.FromSnapshot(package.Snapshot);
            var entry = new CaptureCatalogEntry(temporaryDirectory, identity, package);
            var catalog = new CaptureCatalog(
                [new DeviceCaptureHistory(identity, [entry])],
                []);
            var index = new CaptureIndex(Path.Combine(temporaryDirectory, "test.db"));

            await index.SynchronizeAsync(catalog);
            await index.SynchronizeAsync(catalog);
            var counts = await index.GetCountsAsync();

            Assert.Equal(1, counts.Devices);
            Assert.Equal(1, counts.Captures);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private static AcquisitionPackage CreatePackage()
    {
        const string json = """
        {
          "device": { "name": "/dev/test", "type": "ata", "protocol": "ATA" },
          "model_name": "SYNTHETIC ATA",
          "serial_number": "SYNTHETIC-STORAGE-001",
          "smart_status": { "passed": true },
          "power_on_time": { "hours": 10 },
          "temperature": { "current": 25 }
        }
        """;
        var timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        return new AcquisitionPackage(
            1,
            "synthetic-capture",
            timestamp,
            new AcquisitionCommandMetadata("smartctl", [], timestamp, timestamp, 0, false),
            SmartctlResultAssessment.FromExitCode(0),
            SmartctlJsonParser.ParseSnapshot(json));
    }
}
