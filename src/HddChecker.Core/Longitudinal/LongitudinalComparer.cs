using HddChecker.Core.Acquisition;

namespace HddChecker.Core.Longitudinal;

public static class LongitudinalComparer
{
    private static readonly HashSet<string> RateMetricNames =
    [
        "Ciclos de encendido",
        "Ciclos de carga",
        "Unidades NVMe leídas",
        "Unidades NVMe escritas",
        "Apagados inseguros NVMe",
        "Errores de medios NVMe",
        "Entradas de errores NVMe",
        "Sectores reasignados",
        "Sectores irrecuperables",
        "Errores no corregibles",
        "Errores CRC SATA",
    ];

    public static LongitudinalComparison Compare(
        AcquisitionPackage first,
        AcquisitionPackage second)
    {
        var previous = first.CapturedAtUtc <= second.CapturedAtUtc ? first : second;
        var current = ReferenceEquals(previous, first) ? second : first;
        var previousIdentity = DeviceIdentity.FromSnapshot(previous.Snapshot);
        var currentIdentity = DeviceIdentity.FromSnapshot(current.Snapshot);

        if (!DeviceIdentity.ShareIdentifier(previous.Snapshot, current.Snapshot))
        {
            throw new InvalidOperationException(
                $"Las capturas pertenecen a unidades diferentes ({previousIdentity.Key} y {currentIdentity.Key}).");
        }

        var comparisonIdentity = DeviceIdentity.SelectPreferred(
            DeviceIdentity.GetCandidates(previous.Snapshot)
                .Concat(DeviceIdentity.GetCandidates(current.Snapshot)));

        var before = previous.Snapshot;
        var after = current.Snapshot;
        var metrics = new[]
        {
            MetricChange.Create("Horas de funcionamiento", before.PowerOnHours, after.PowerOnHours, "h"),
            MetricChange.Create("Ciclos de encendido", before.PowerCycleCount, after.PowerCycleCount),
            MetricChange.Create("Ciclos de carga", before.LoadCycleCount, after.LoadCycleCount),
            MetricChange.Create("Temperatura", before.TemperatureCelsius, after.TemperatureCelsius, "°C"),
            MetricChange.Create("Vida restante", before.RemainingLifePercent, after.RemainingLifePercent, "%"),
            MetricChange.Create("Reserva NVMe disponible", before.NvmeAvailableSparePercent, after.NvmeAvailableSparePercent, "%"),
            MetricChange.Create("Unidades NVMe leídas", before.NvmeDataUnitsRead, after.NvmeDataUnitsRead),
            MetricChange.Create("Unidades NVMe escritas", before.NvmeDataUnitsWritten, after.NvmeDataUnitsWritten),
            MetricChange.Create("Apagados inseguros NVMe", before.NvmeUnsafeShutdowns, after.NvmeUnsafeShutdowns),
            MetricChange.Create("Errores de medios NVMe", before.NvmeMediaErrors, after.NvmeMediaErrors),
            MetricChange.Create("Entradas de errores NVMe", before.NvmeErrorLogEntries, after.NvmeErrorLogEntries),
            MetricChange.Create("Sectores reasignados", before.ReallocatedSectors, after.ReallocatedSectors),
            MetricChange.Create("Sectores pendientes", before.PendingSectors, after.PendingSectors),
            MetricChange.Create("Sectores irrecuperables", before.OfflineUncorrectableSectors, after.OfflineUncorrectableSectors),
            MetricChange.Create("Errores no corregibles", before.ReportedUncorrectableErrors, after.ReportedUncorrectableErrors),
            MetricChange.Create("Errores CRC SATA", before.SataCrcErrors, after.SataCrcErrors),
        };

        var calendarElapsed = current.CapturedAtUtc - previous.CapturedAtUtc;
        var powerOnHoursElapsed = metrics
            .First(metric => metric.Name == "Horas de funcionamiento")
            .Delta;
        var rates = metrics
            .Where(metric =>
                metric.Delta is not null &&
                RateMetricNames.Contains(metric.Name))
            .Select(metric => new MetricRate(
                metric.Name,
                metric.Delta!.Value,
                powerOnHoursElapsed > 0
                    ? metric.Delta.Value / (double)powerOnHoursElapsed.Value
                    : null,
                calendarElapsed >= TimeSpan.FromDays(1)
                    ? metric.Delta.Value / calendarElapsed.TotalDays
                    : null,
                metric.Unit))
            .ToArray();

        return new LongitudinalComparison(
            comparisonIdentity,
            previous.CapturedAtUtc,
            current.CapturedAtUtc,
            calendarElapsed,
            metrics,
            rates);
    }
}
