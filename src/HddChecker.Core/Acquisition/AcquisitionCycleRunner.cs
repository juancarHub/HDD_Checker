using System.Text.Json;
using HddChecker.Core.Smartctl;
using HddChecker.Core.Longitudinal;

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
        var ignoredExternalDevices = devices
            .Where(DeviceMonitoringPolicy.IsExternal)
            .ToArray();
        var monitoredDevices = devices
            .Where(device => !DeviceMonitoringPolicy.IsExternal(device))
            .ToArray();
        var attempts = new List<DeviceAcquisitionAttempt>(devices.Count);
        var capturedIdentities = new HashSet<DeviceIdentity>();
        foreach (var device in monitoredDevices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                attempts.Add(await AcquireDeviceAsync(
                    device,
                    capturesRoot,
                    capturedIdentities,
                    cancellationToken));
            }
            catch (SmartctlTimeoutException exception)
            {
                attempts.Add(new DeviceAcquisitionAttempt(
                    device,
                    DeviceAcquisitionStatus.Failed,
                    AcquisitionOutcome.Fatal,
                    null,
                    exception.Message,
                    null));
            }
            catch (Exception exception) when (
                exception is JsonException or InvalidDataException or IOException)
            {
                attempts.Add(new DeviceAcquisitionAttempt(
                    device,
                    DeviceAcquisitionStatus.Failed,
                    AcquisitionOutcome.Fatal,
                    null,
                    exception.Message,
                    null));
            }
        }

        return new AcquisitionCycleResult(
            startedAt,
            DateTimeOffset.UtcNow,
            attempts,
            ignoredExternalDevices);
    }

    private async Task<DeviceAcquisitionAttempt> AcquireDeviceAsync(
        SmartctlDevice device,
        string capturesRoot,
        ISet<DeviceIdentity> capturedIdentities,
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
                DeviceAcquisitionStatus.Failed,
                assessment.Outcome,
                null,
                ReadError(result),
                null);
        }

        var snapshot = SmartctlJsonParser.ParseSnapshot(result.StandardOutput);
        var identity = DeviceIdentity.FromSnapshot(snapshot);
        if (!capturedIdentities.Add(identity))
        {
            return new DeviceAcquisitionAttempt(
                device,
                DeviceAcquisitionStatus.DuplicateRoute,
                assessment.Outcome,
                null,
                "La unidad física ya fue capturada mediante otra ruta en este ciclo.",
                identity.Key);
        }

        var directory = await packageWriter.WriteAsync(
            capturesRoot,
            result,
            assessment,
            snapshot,
            cancellationToken);
        return new DeviceAcquisitionAttempt(
            device,
            DeviceAcquisitionStatus.Captured,
            assessment.Outcome,
            directory,
            null,
            identity.Key);
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
