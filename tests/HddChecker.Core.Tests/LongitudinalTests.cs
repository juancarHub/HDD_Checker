using HddChecker.Core.Acquisition;
using HddChecker.Core.Longitudinal;
using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Tests;

public sealed class LongitudinalTests
{
    [Fact]
    public void IdentityPrefersWwnOverSerialAndPath()
    {
        var package = CreatePackage("SERIAL-A", "5-000-test", 10, 30, 0, DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        var identity = DeviceIdentity.FromSnapshot(package.Snapshot);

        Assert.Equal("wwn:5000TEST", identity.Key);
    }

    [Fact]
    public void ComparisonCalculatesCounterDeltasForSameDevice()
    {
        var previous = CreatePackage("SERIAL-A", null, 10, 30, 1, DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var current = CreatePackage("SERIAL-A", null, 12, 32, 4, DateTimeOffset.Parse("2026-01-02T00:00:00Z"));

        var comparison = LongitudinalComparer.Compare(previous, current);

        Assert.Equal(2, comparison.PowerOnHoursElapsed);
        Assert.Equal(2, comparison.Metrics.Single(metric => metric.Name == "Temperatura").Delta);
        Assert.Equal(3, comparison.Metrics.Single(metric => metric.Name == "Errores CRC SATA").Delta);
    }

    [Fact]
    public void ComparisonRejectsDifferentPhysicalDevices()
    {
        var first = CreatePackage("SERIAL-A", null, 10, 30, 0, DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var second = CreatePackage("SERIAL-B", null, 10, 30, 0, DateTimeOffset.Parse("2026-01-02T00:00:00Z"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            LongitudinalComparer.Compare(first, second));

        Assert.Contains("unidades diferentes", exception.Message);
    }

    private static AcquisitionPackage CreatePackage(
        string serial,
        string? wwn,
        long hours,
        int temperature,
        long crcErrors,
        DateTimeOffset capturedAt)
    {
        var wwnJson = wwn is null
            ? string.Empty
            : $"\"wwn\": \"{wwn}\",";
        var json = $$"""
        {
          "device": { "name": "/dev/changing-path", "type": "ata", "protocol": "ATA" },
          "model_name": "SYNTHETIC DEVICE",
          "serial_number": "{{serial}}",
          {{wwnJson}}
          "smart_status": { "passed": true },
          "power_on_time": { "hours": {{hours}} },
          "temperature": { "current": {{temperature}} },
          "ata_smart_attributes": {
            "table": [
              { "id": 199, "name": "UDMA_CRC_Error_Count", "value": 200, "worst": 200, "thresh": 0, "raw": { "value": {{crcErrors}}, "string": "{{crcErrors}}" } }
            ]
          }
        }
        """;
        var snapshot = SmartctlJsonParser.ParseSnapshot(json);
        var command = new AcquisitionCommandMetadata(
            "smartctl",
            ["--all", "--json=c"],
            capturedAt,
            capturedAt.AddSeconds(1),
            0,
            false);

        return new AcquisitionPackage(
            1,
            capturedAt.ToString("yyyyMMddTHHmmssZ"),
            capturedAt,
            command,
            SmartctlResultAssessment.FromExitCode(0),
            snapshot);
    }
}
