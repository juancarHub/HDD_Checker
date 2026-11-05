using System.Text.Json;
using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Acquisition;

public sealed class AcquisitionCycleRunner(
    ISmartctlRunner smartctl,
    AcquisitionPackageWriter packageWriter)
{
    public async Task<AcquisitionCycleResult> RunAsync(
        string capturesRoot,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var scan = await smartctl.RunAsync(["--scan", "--json=c"], cancellationToken);
        var scanAssessment = SmartctlResultAssessment.FromExitCode(scan.ExitCode);
        if (scanAssessment.Outcome is AcquisitionOutcome.Fatal)
        {
            throw new InvalidOperationException(
                "El descubrimiento de dispositivos falló: " + ReadError(scan));
        }

        var devices = SmartctlJsonParser.ParseScan(scan.StandardOutput);
        var attempts = new List<DeviceAcquisitionAttempt>(devices.Count);
        foreach (var device in devices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                attempts.Add(await AcquireDeviceAsync(device, capturesRoot, cancellationToken));
            }
            catch (SmartctlTimeoutException exception)
            {
                attempts.Add(new DeviceAcquisitionAttempt(
                    device,
                    AcquisitionOutcome.Fatal,
                    null,
                    exception.Message));
            }
            catch (Exception exception) when (
                exception is JsonException or InvalidDataException or IOException)
            {
                attempts.Add(new DeviceAcquisitionAttempt(
                    device,
                    AcquisitionOutcome.Fatal,
                    null,
                    exception.Message));
            }
        }

        return new AcquisitionCycleResult(startedAt, DateTimeOffset.UtcNow, attempts);
    }

    private async Task<DeviceAcquisitionAttempt> AcquireDeviceAsync(
        SmartctlDevice device,
        string capturesRoot,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>();
        if (!string.IsNullOrWhiteSpace(device.Type))
        {
            arguments.Add("--device");
            arguments.Add(device.Type);
        }

        arguments.Add("--all");
        arguments.Add("--json=c");
        arguments.Add(device.Name);

        var result = await smartctl.RunAsync(arguments, cancellationToken);
        var assessment = SmartctlResultAssessment.FromExitCode(result.ExitCode);
        if (assessment.Outcome is AcquisitionOutcome.Fatal)
        {
            return new DeviceAcquisitionAttempt(
                device,
                assessment.Outcome,
                null,
                ReadError(result));
        }

        var snapshot = SmartctlJsonParser.ParseSnapshot(result.StandardOutput);
        var directory = await packageWriter.WriteAsync(
            capturesRoot,
            result,
            assessment,
            snapshot,
            cancellationToken);
        return new DeviceAcquisitionAttempt(device, assessment.Outcome, directory, null);
    }

    private static string ReadError(SmartctlCommandResult result)
    {
        try
        {
            var messages = SmartctlJsonParser.ParseMessages(result.StandardOutput);
            if (messages.Count > 0)
            {
                return string.Join("; ", messages);
            }
        }
        catch (JsonException)
        {
            // Fall back to stderr or a generic process result below.
        }

        return !string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardError.Trim()
            : $"smartctl terminó con código {result.ExitCode}.";
    }
}
