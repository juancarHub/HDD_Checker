using HddChecker.Core.Acquisition;
using HddChecker.Core.Analysis;
using HddChecker.Core.Longitudinal;
using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Tests;

public sealed class TrendRuleEvaluatorTests
{
    [Fact]
    public void UnsafeShutdownIncreaseIsInformationalWhileMediaErrorIsWarning()
    {
        var previous = CreateNvmePackage(10, 0, true, DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var current = CreateNvmePackage(11, 1, true, DateTimeOffset.Parse("2026-01-02T00:00:00Z"));
        var comparison = LongitudinalComparer.Compare(previous, current);

        var findings = TrendRuleEvaluator.Evaluate(previous, current, comparison);

        Assert.Contains(findings, finding =>
            finding.Code == "nvme.unsafe-shutdown.increase" &&
            finding.Severity == FindingSeverity.Information);
        Assert.Contains(findings, finding =>
            finding.Code == "nvme.media-errors.increase" &&
            finding.Severity == FindingSeverity.Warning);
    }

    [Fact]
    public void SmartFailureIsCritical()
    {
        var previous = CreateNvmePackage(10, 0, true, DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var current = CreateNvmePackage(10, 0, false, DateTimeOffset.Parse("2026-01-02T00:00:00Z"));
        var comparison = LongitudinalComparer.Compare(previous, current);

        var findings = TrendRuleEvaluator.Evaluate(previous, current, comparison);

        Assert.Contains(findings, finding =>
            finding.Code == "smart.status.failed" &&
            finding.Severity == FindingSeverity.Critical);
    }

    private static AcquisitionPackage CreateNvmePackage(
        long unsafeShutdowns,
        long mediaErrors,
        bool smartPassed,
        DateTimeOffset capturedAt)
    {
        var json = $$"""
        {
          "device": { "name": "/dev/test", "type": "nvme", "protocol": "NVMe" },
          "model_name": "SYNTHETIC NVME",
          "serial_number": "SYNTHETIC-001",
          "smart_status": { "passed": {{smartPassed.ToString().ToLowerInvariant()}} },
          "power_on_time": { "hours": 100 },
          "temperature": { "current": 30 },
          "endurance_used": { "current_percent": 1 },
          "nvme_smart_health_information_log": {
            "available_spare": 100,
            "data_units_read": 100,
            "data_units_written": 200,
            "unsafe_shutdowns": {{unsafeShutdowns}},
            "media_errors": {{mediaErrors}},
            "num_err_log_entries": 0
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
