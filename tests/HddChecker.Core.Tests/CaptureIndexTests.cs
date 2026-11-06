using HddChecker.Core.Acquisition;
using HddChecker.Core.Analysis;
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
            var statuses = await index.GetStatusesAsync("SYNTHETIC");
            var measurements = await index.GetMeasurementsAsync(identity.Key);

            Assert.Equal(1, counts.Devices);
            Assert.Equal(1, counts.Captures);
            var status = Assert.Single(statuses);
            Assert.Equal("SYNTHETIC ATA", status.ModelName);
            Assert.True(status.SmartPassed);
            var measurement = Assert.Single(measurements);
            Assert.Equal("synthetic-capture", measurement.CaptureId);
            Assert.Equal(25, measurement.TemperatureCelsius);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task WarningIsQueuedOnceAndCanBeMarkedDelivered()
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"hdd-checker-alert-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            var package = CreateWarningPackage();
            var identity = DeviceIdentity.FromSnapshot(package.Snapshot);
            var entry = new CaptureCatalogEntry(temporaryDirectory, identity, package);
            var catalog = new CaptureCatalog(
                [new DeviceCaptureHistory(identity, [entry])],
                []);
            var index = new CaptureIndex(Path.Combine(temporaryDirectory, "test.db"));

            await index.SynchronizeAsync(catalog);
            Assert.Equal(1, await index.QueueLatestNotificationsAsync());
            Assert.Equal(0, await index.QueueLatestNotificationsAsync());
            var notification = Assert.Single(await index.GetPendingNotificationsAsync());
            Assert.Equal(FindingSeverity.Warning, notification.Severity);

            await index.MarkNotificationDeliveredAsync(notification.EventKey);

            Assert.Empty(await index.GetPendingNotificationsAsync());
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

    private static AcquisitionPackage CreateWarningPackage()
    {
        const string json = """
        {
          "device": { "name": "/dev/test", "type": "ata", "protocol": "ATA" },
          "model_name": "SYNTHETIC WARNING ATA",
          "serial_number": "SYNTHETIC-WARNING-001",
          "smart_status": { "passed": true },
          "power_on_time": { "hours": 20 },
          "temperature": { "current": 30 },
          "ata_smart_attributes": {
            "table": [
              { "id": 197, "name": "Current_Pending_Sector", "value": 100,
                "worst": 100, "thresh": 0, "raw": { "value": 2, "string": "2" } }
            ]
          }
        }
        """;
        var timestamp = DateTimeOffset.Parse("2026-01-02T00:00:00Z");
        return new AcquisitionPackage(
            1,
            "synthetic-warning-capture",
            timestamp,
            new AcquisitionCommandMetadata("smartctl", [], timestamp, timestamp, 0, false),
            SmartctlResultAssessment.FromExitCode(0),
            SmartctlJsonParser.ParseSnapshot(json));
    }
}
