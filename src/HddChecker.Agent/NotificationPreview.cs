namespace HddChecker.Agent;

internal sealed record NotificationPreview(
    string Title,
    string Message,
    string Recommendation,
    HddChecker.Core.Analysis.FindingSeverity? Severity,
    ToolTipIcon Icon)
{
    public static IReadOnlyList<NotificationPreview> All { get; } =
    [
        new(
            "[SIMULACIÓN] Supervisión activa",
            "La supervisión está funcionando y no se han detectado incidencias nuevas.",
            "No es necesaria ninguna acción.",
            null,
            ToolTipIcon.Info),
        new(
            "[SIMULACIÓN] Indicador que requiere atención",
            "KINGSTON SA400S37480G: se ha observado un cambio que conviene vigilar. " +
            "Mantén la supervisión y abre el panel para consultar la trayectoria.",
            "Observa si el indicador continúa aumentando en las próximas mediciones.",
            HddChecker.Core.Analysis.FindingSeverity.Attention,
            ToolTipIcon.Info),
        new(
            "[SIMULACIÓN] Advertencia de almacenamiento",
            "Hitachi HDT721010SLA360: han aumentado los sectores pendientes. " +
            "Comprueba la copia de seguridad y revisa esta unidad lo antes posible.",
            "Comprueba la copia de seguridad y revisa esta unidad lo antes posible.",
            HddChecker.Core.Analysis.FindingSeverity.Warning,
            ToolTipIcon.Warning),
        new(
            "[SIMULACIÓN] Problema crítico de almacenamiento",
            "KINGSTON SA400S37480G: la unidad informa un fallo SMART actual. " +
            "Comprueba la copia de seguridad y planifica su sustitución inmediata.",
            "Comprueba la copia de seguridad y planifica la sustitución inmediata.",
            HddChecker.Core.Analysis.FindingSeverity.Critical,
            ToolTipIcon.Error),
    ];
}
