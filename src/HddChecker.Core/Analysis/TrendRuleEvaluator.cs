using HddChecker.Core.Acquisition;
using HddChecker.Core.Longitudinal;
using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Analysis;

public static class TrendRuleEvaluator
{
    private static readonly IReadOnlyDictionary<string, (string Code, FindingSeverity Severity, string Message)> IncreaseRules =
        new Dictionary<string, (string, FindingSeverity, string)>
        {
            ["Sectores reasignados"] = ("ata.reallocated.increase", FindingSeverity.Warning, "Han aumentado los sectores reasignados."),
            ["Sectores pendientes"] = ("ata.pending.increase", FindingSeverity.Warning, "Han aumentado los sectores pendientes."),
            ["Sectores irrecuperables"] = ("ata.uncorrectable.increase", FindingSeverity.Warning, "Han aumentado los sectores irrecuperables."),
            ["Errores no corregibles"] = ("ata.reported-uncorrectable.increase", FindingSeverity.Warning, "Han aumentado los errores no corregibles."),
            ["Errores de medios NVMe"] = ("nvme.media-errors.increase", FindingSeverity.Warning, "Han aumentado los errores físicos de medios NVMe."),
            ["Errores CRC SATA"] = ("ata.crc.increase", FindingSeverity.Attention, "Han aumentado los errores CRC; revisar conexión, cable o controladora."),
            ["Entradas de errores NVMe"] = ("nvme.error-log.increase", FindingSeverity.Attention, "Ha aumentado el registro de errores NVMe; se necesita contexto adicional."),
            ["Apagados inseguros NVMe"] = ("nvme.unsafe-shutdown.increase", FindingSeverity.Information, "Se ha registrado al menos un apagado inseguro NVMe."),
        };

    public static IReadOnlyList<TrendFinding> Evaluate(
        AcquisitionPackage previous,
        AcquisitionPackage current,
        LongitudinalComparison comparison)
    {
        var findings = new List<TrendFinding>();

        if (previous.Snapshot.SmartPassed is not false && current.Snapshot.SmartPassed is false)
        {
            findings.Add(new TrendFinding(
                "smart.status.failed",
                FindingSeverity.Critical,
                "El estado SMART ha cambiado a fallo."));
        }

        var previousCurrentThresholds = previous.Snapshot.ThresholdEvents
            .Where(IsCurrentThresholdFailure)
            .Select(attribute => attribute.Id)
            .ToHashSet();
        foreach (var attribute in current.Snapshot.ThresholdEvents.Where(IsCurrentThresholdFailure))
        {
            if (previousCurrentThresholds.Contains(attribute.Id))
            {
                continue;
            }

            findings.Add(new TrendFinding(
                "ata.threshold.current",
                FindingSeverity.Critical,
                $"El atributo {attribute.Id} {attribute.Name} ha alcanzado el umbral actual."));
        }

        foreach (var metric in comparison.Metrics.Where(metric => metric.Delta > 0))
        {
            if (!IncreaseRules.TryGetValue(metric.Name, out var rule))
            {
                continue;
            }

            findings.Add(new TrendFinding(
                rule.Code,
                rule.Severity,
                $"{rule.Message} Incremento: +{metric.Delta}."));
        }

        AddDecreaseFinding(
            findings,
            comparison,
            "Vida restante",
            "ssd.life.decrease",
            FindingSeverity.Information,
            "Ha disminuido la estimación de vida restante.");
        AddDecreaseFinding(
            findings,
            comparison,
            "Reserva NVMe disponible",
            "nvme.spare.decrease",
            FindingSeverity.Attention,
            "Ha disminuido la reserva disponible del NVMe.");

        foreach (var metric in comparison.Metrics.Where(IsUnexpectedCounterDecrease))
        {
            findings.Add(new TrendFinding(
                "counter.decrease.unexpected",
                FindingSeverity.Attention,
                $"El contador {metric.Name} ha disminuido ({metric.Previous} → {metric.Current}); revisar reinicio, sustitución o interpretación."));
        }

        return findings
            .OrderByDescending(finding => finding.Severity)
            .ThenBy(finding => finding.Code)
            .ToArray();
    }

    private static bool IsCurrentThresholdFailure(SmartAttributeThresholdEvent attribute) =>
        !attribute.WhenFailed.Equals("past", StringComparison.OrdinalIgnoreCase);

    private static void AddDecreaseFinding(
        ICollection<TrendFinding> findings,
        LongitudinalComparison comparison,
        string metricName,
        string code,
        FindingSeverity severity,
        string message)
    {
        var metric = comparison.Metrics.First(item => item.Name == metricName);
        if (metric.Delta >= 0)
        {
            return;
        }

        findings.Add(new TrendFinding(
            code,
            severity,
            $"{message} Cambio: {metric.Delta} puntos."));
    }

    private static bool IsUnexpectedCounterDecrease(MetricChange metric) =>
        metric.Delta < 0 && metric.Name is
            "Horas de funcionamiento" or
            "Ciclos de encendido" or
            "Ciclos de carga" or
            "Unidades NVMe leídas" or
            "Unidades NVMe escritas" or
            "Apagados inseguros NVMe" or
            "Errores de medios NVMe" or
            "Entradas de errores NVMe" or
            "Sectores reasignados" or
            "Sectores pendientes" or
            "Sectores irrecuperables" or
            "Errores no corregibles" or
            "Errores CRC SATA";
}
