using HddChecker.Core.Acquisition;
using HddChecker.Core.Smartctl;
using HddChecker.Storage;
using Microsoft.Extensions.Options;

namespace HddChecker.Service;

public sealed class MonitoringWorker(
    IOptions<MonitoringOptions> options,
    ILogger<MonitoringWorker> logger) : BackgroundService
{
    private readonly MonitoringOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var dataRoot = ResolveDataRoot(options.DataRoot);
        var capturesRoot = Path.Combine(dataRoot, "captures");
        var databasePath = Path.Combine(dataRoot, "data", "hdd-checker.db");
        var interval = TimeSpan.FromMinutes(options.IntervalMinutes);

        logger.LogInformation(
            "Monitor iniciado. Intervalo: {Interval}. Datos: {DataRoot}",
            interval,
            dataRoot);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(capturesRoot, databasePath, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "El ciclo de adquisición no pudo completarse.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Monitor detenido.");
    }

    private async Task RunCycleAsync(
        string capturesRoot,
        string databasePath,
        CancellationToken cancellationToken)
    {
        var smartctl = new SmartctlRunner(
            options.SmartctlPath,
            TimeSpan.FromSeconds(options.TimeoutSeconds));
        var cycle = await new AcquisitionCycleRunner(smartctl, new AcquisitionPackageWriter())
            .RunAsync(capturesRoot, cancellationToken);

        foreach (var attempt in cycle.Attempts)
        {
            if (attempt.Status is DeviceAcquisitionStatus.Failed)
            {
                logger.LogWarning(
                    "No se pudo capturar {Device} ({Type}): {Error}",
                    attempt.Device.Name,
                    attempt.Device.Type,
                    attempt.ErrorMessage);
            }
            else if (attempt.Status is DeviceAcquisitionStatus.DuplicateRoute)
            {
                logger.LogInformation(
                    "Ruta duplicada omitida: {Device} ({Identity}).",
                    attempt.Device.Name,
                    attempt.IdentityKey);
            }
        }

        var catalog = await CaptureCatalog.LoadAsync(capturesRoot, cancellationToken);
        var index = new CaptureIndex(databasePath);
        var summary = await index.SynchronizeAsync(catalog, cancellationToken);
        var queuedNotifications = await index.QueueLatestNotificationsAsync(cancellationToken);

        logger.LogInformation(
            "Ciclo completado: {Captured} capturados, {Failed} fallidos, " +
            "{Duplicates} rutas duplicadas, {External} externos USB ignorados, " +
            "{Devices} unidades indexadas y " +
            "{Issues} incidencias de catálogo y {Notifications} alertas nuevas.",
            cycle.CapturedCount,
            cycle.FailedCount,
            cycle.DuplicateRouteCount,
            cycle.IgnoredExternalCount,
            summary.DeviceCount,
            summary.IssueCount,
            queuedNotifications);
    }

    private static string ResolveDataRoot(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "HDD_Checker");
    }
}
