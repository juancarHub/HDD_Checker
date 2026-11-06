using HddChecker.Core.Acquisition;
using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Tests;

public sealed class CaptureCatalogTests
{
    [Fact]
    public async Task SerialOnlyCaptureIsMergedWithLaterSerialAndWwnCapture()
    {
        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            $"hdd-checker-catalog-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            await WriteCaptureAsync(
                temporaryDirectory,
                DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                includeWwn: false);
            await WriteCaptureAsync(
                temporaryDirectory,
                DateTimeOffset.Parse("2026-01-02T00:00:00Z"),
                includeWwn: true);

            var catalog = await CaptureCatalog.LoadAsync(temporaryDirectory);

            var device = Assert.Single(catalog.Devices);
            Assert.Equal("wwn:5000TEST", device.Identity.Key);
            Assert.Equal(2, device.Captures.Count);
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private static async Task WriteCaptureAsync(
        string capturesRoot,
        DateTimeOffset timestamp,
        bool includeWwn)
    {
        var wwn = includeWwn ? "\"wwn\": \"5-000-test\"," : string.Empty;
        var json = $$"""
        {
          "device": { "name": "/dev/test", "type": "ata", "protocol": "ATA" },
          "model_name": "SYNTHETIC DEVICE",
          "serial_number": "SERIAL-A",
          {{wwn}}
          "smart_status": { "passed": true },
          "power_on_time": { "hours": 10 }
        }
        """;
        var command = new SmartctlCommandResult(
            "smartctl",
            ["--all", "--json=c", "/dev/test"],
            timestamp,
            timestamp.AddSeconds(1),
            0,
            json,
            string.Empty);
        await new AcquisitionPackageWriter().WriteAsync(
            capturesRoot,
            command,
            SmartctlResultAssessment.FromExitCode(0),
            SmartctlJsonParser.ParseSnapshot(json));
    }
}
