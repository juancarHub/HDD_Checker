using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Analysis;

public static class SnapshotRuleEvaluator
{
    public static IReadOnlyList<TrendFinding> Evaluate(SmartDeviceSnapshot snapshot)
    {
        var findings = new List<TrendFinding>();

        if (snapshot.SmartPassed is false)
        {
            findings.Add(new TrendFinding(
                "snapshot.smart.failed",
                FindingSeverity.Critical,
                "El dispositivo informa fallo SMART actual."));
        }

        AddPositiveValue(
            findings,
            snapshot.PendingSectors,
            "snapshot.ata.pending",
            FindingSeverity.Warning,
            "La medición contiene sectores pendientes");
        AddPositiveValue(
            findings,
            snapshot.OfflineUncorrectableSectors,
            "snapshot.ata.uncorrectable",
            FindingSeverity.Warning,
            "La medición contiene sectores irrecuperables");
        AddPositiveValue(
            findings,
            snapshot.NvmeMediaErrors,
            "snapshot.nvme.media-errors",
            FindingSeverity.Warning,
            "La medición contiene errores físicos de medios NVMe");
        AddPositiveValue(
            findings,
            snapshot.ReallocatedSectors,
            "snapshot.ata.reallocated",
            FindingSeverity.Attention,
            "La medición contiene sectores reasignados");

        foreach (var attribute in snapshot.ThresholdEvents)
        {
            var isPast = attribute.WhenFailed.Equals("past", StringComparison.OrdinalIgnoreCase);
            findings.Add(new TrendFinding(
                isPast ? "snapshot.ata.threshold.past" : "snapshot.ata.threshold.current",
                isPast ? FindingSeverity.Information : FindingSeverity.Critical,
                $"El atributo {attribute.Id} {attribute.Name} " +
                (isPast ? "alcanzó el umbral en el pasado." : "ha alcanzado el umbral actual.")));
        }

        return findings
            .OrderByDescending(finding => finding.Severity)
            .ThenBy(finding => finding.Code)
            .ToArray();
    }

    private static void AddPositiveValue(
        ICollection<TrendFinding> findings,
        long? value,
        string code,
        FindingSeverity severity,
        string message)
    {
        if (value is null or <= 0)
        {
            return;
        }

        findings.Add(new TrendFinding(code, severity, $"{message}: {value}."));
    }
}
