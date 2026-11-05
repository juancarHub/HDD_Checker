using HddChecker.Core.Acquisition;
using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Tests;

public sealed class AcquisitionCycleRunnerTests
{
    [Fact]
    public async Task OneFailedDeviceDoesNotPreventAnotherCapture()
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), $"hdd-checker-cycle-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryDirectory);
        try
        {
            var runner = new SyntheticRunner();
            var cycle = await new AcquisitionCycleRunner(runner, new AcquisitionPackageWriter())
                .RunAsync(temporaryDirectory);

            Assert.Equal(2, cycle.DiscoveredCount);
            Assert.Equal(1, cycle.CapturedCount);
            Assert.Equal(1, cycle.FailedCount);
            Assert.True(cycle.Attempts[0].Succeeded);
            Assert.False(cycle.Attempts[1].Succeeded);
            Assert.Contains("inaccesible", cycle.Attempts[1].ErrorMessage);
            Assert.Single(Directory.EnumerateFiles(
                temporaryDirectory,
                "acquisition.json",
                SearchOption.AllDirectories));
        }
        finally
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private sealed class SyntheticRunner : ISmartctlRunner
    {
        public Task<SmartctlCommandResult> RunAsync(
            IEnumerable<string> arguments,
            CancellationToken cancellationToken = default)
        {
            var values = arguments.ToArray();
            var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
            if (values.Contains("--scan"))
            {
                return Task.FromResult(Result(values, 0, """
                    { "devices": [
                      { "name": "/dev/good", "type": "ata", "protocol": "ATA" },
                      { "name": "/dev/bad", "type": "scsi", "protocol": "SCSI" }
                    ] }
                    """, now));
            }

            if (values.Contains("/dev/good"))
            {
                return Task.FromResult(Result(values, 0, """
                    {
                      "device": { "name": "/dev/good", "type": "ata", "protocol": "ATA" },
                      "model_name": "SYNTHETIC GOOD",
                      "serial_number": "SYNTHETIC-GOOD-001",
                      "smart_status": { "passed": true },
                      "power_on_time": { "hours": 10 },
                      "temperature": { "current": 25 }
                    }
                    """, now));
            }

            return Task.FromResult(Result(values, 2, """
                { "smartctl": { "messages": [
                  { "string": "dispositivo inaccesible", "severity": "error" }
                ] } }
                """, now));
        }

        private static SmartctlCommandResult Result(
            IReadOnlyList<string> arguments,
            int exitCode,
            string output,
            DateTimeOffset timestamp) =>
            new(
                "synthetic-smartctl",
                arguments,
                timestamp,
                timestamp.AddSeconds(1),
                exitCode,
                output,
                string.Empty);
    }
}
