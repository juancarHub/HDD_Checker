using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Analysis;

public static class DeviceHealthAssessmentEvaluator
{
    public static DeviceHealthAssessment Evaluate(DeviceHealthSignals signals)
    {
        var reasons = BuildReasons(signals);
        var limitations = BuildLimitations(signals);
        var severity = signals.SmartPassed is false
            ? FindingSeverity.Critical
            : signals.HighestFindingSeverity;

        var diagnosis = severity switch
        {
            FindingSeverity.Critical => "Problema crítico detectado",
            FindingSeverity.Warning => "Problema que requiere atención",
            FindingSeverity.Attention => "Indicadores que conviene vigilar",
            FindingSeverity.Information => "Sin problema actual; existe información relevante",
            _ when signals.SmartPassed is true => "Sin indicios observados de problema",
            _ => "Estado actual no concluyente"
        };

        var confidence = DetermineConfidence(signals, limitations);
        var trajectory = signals.CaptureCount switch
        {
            <= 0 => "No evaluable: todavía no hay mediciones.",
            1 => "Aún no evaluable: solo existe una medición.",
            _ when signals.HighestFindingSeverity is null =>
                $"Disponible ({signals.CaptureCount} mediciones), sin cambios recientes relevantes.",
            _ => $"Disponible ({signals.CaptureCount} mediciones), con un cambio reciente registrado."
        };

        var recommendation = severity switch
        {
            FindingSeverity.Critical =>
                "Comprueba la copia de seguridad y planifica la sustitución inmediata de la unidad.",
            FindingSeverity.Warning =>
                "Comprueba la copia de seguridad y revisa esta unidad lo antes posible.",
            FindingSeverity.Attention =>
                "Mantén la supervisión y revisa si el indicador continúa aumentando.",
            _ => "No es necesaria ninguna acción inmediata; continúa la supervisión periódica."
        };

        return new DeviceHealthAssessment(
            diagnosis,
            confidence,
            reasons,
            limitations,
            trajectory,
            recommendation);
    }

    private static List<string> BuildReasons(DeviceHealthSignals signals)
    {
        var reasons = new List<string>();
        if (signals.SmartPassed is true)
        {
            reasons.Add("La comprobación general SMART está aprobada.");
        }
        else if (signals.SmartPassed is false)
        {
            reasons.Add("La unidad informa un fallo en la comprobación general SMART.");
        }

        if (signals.RemainingLifePercent is int life)
        {
            reasons.Add($"La unidad declara un {life} % de vida restante.");
        }

        if (signals.TemperatureCelsius is int temperature)
        {
            reasons.Add($"La temperatura observada es de {temperature} °C.");
        }

        if (signals.HighestFindingSeverity is not null &&
            !string.IsNullOrWhiteSpace(signals.HighestFindingMessage))
        {
            reasons.Add(signals.HighestFindingMessage);
        }
        else
        {
            AddKnownZero(reasons, signals.PendingSectors, "sectores pendientes");
            AddKnownZero(reasons, signals.ReallocatedSectors, "sectores reasignados");
            AddKnownZero(reasons, signals.NvmeMediaErrors, "errores físicos de medios NVMe");
        }

        return reasons;
    }

    private static List<string> BuildLimitations(DeviceHealthSignals signals)
    {
        var limitations = new List<string>();
        if (signals.AcquisitionOutcome is not AcquisitionOutcome.Complete)
        {
            limitations.Add("La adquisición SMART fue parcial.");
        }

        if (signals.SmartPassed is null)
        {
            limitations.Add("La unidad no informó un resultado general SMART.");
        }

        if (signals.Protocol?.Equals("ATA", StringComparison.OrdinalIgnoreCase) is true)
        {
            if (signals.ReallocatedSectors is null)
            {
                limitations.Add("La unidad no informa el contador de sectores reasignados.");
            }

            if (signals.PendingSectors is null)
            {
                limitations.Add("La unidad no informa el contador de sectores pendientes.");
            }
        }
        else if (signals.Protocol?.Equals("NVMe", StringComparison.OrdinalIgnoreCase) is true)
        {
            if (signals.NvmeMediaErrors is null)
            {
                limitations.Add("La unidad no informa el contador de errores de medios NVMe.");
            }

            if (signals.NvmeAvailableSparePercent is null)
            {
                limitations.Add("La unidad no informa la reserva disponible NVMe.");
            }
        }

        return limitations;
    }

    private static AssessmentConfidence DetermineConfidence(
        DeviceHealthSignals signals,
        IReadOnlyCollection<string> limitations)
    {
        if (signals.SmartPassed is null || signals.AcquisitionOutcome is AcquisitionOutcome.Fatal)
        {
            return AssessmentConfidence.Limited;
        }

        return limitations.Count == 0
            ? AssessmentConfidence.High
            : AssessmentConfidence.Partial;
    }

    private static void AddKnownZero(ICollection<string> reasons, long? value, string label)
    {
        if (value is 0)
        {
            reasons.Add($"No se registran {label}.");
        }
    }
}
