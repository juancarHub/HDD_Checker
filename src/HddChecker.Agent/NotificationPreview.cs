namespace HddChecker.Agent;

internal sealed record NotificationPreview(
    string Title,
    string Message,
    ToolTipIcon Icon)
{
    public static IReadOnlyList<NotificationPreview> All { get; } =
    [
        new(
            "[SIMULACIÓN] Supervisión activa",
            "La supervisión está funcionando y no se han detectado incidencias nuevas.",
            ToolTipIcon.Info),
        new(
            "[SIMULACIÓN] Indicador que requiere atención",
            "KINGSTON SA400S37480G: se ha observado un cambio que conviene vigilar. " +
            "Mantén la supervisión y abre el panel para consultar la trayectoria.",
            ToolTipIcon.Info),
        new(
            "[SIMULACIÓN] Advertencia de almacenamiento",
            "Hitachi HDT721010SLA360: han aumentado los sectores pendientes. " +
            "Comprueba la copia de seguridad y revisa esta unidad lo antes posible.",
            ToolTipIcon.Warning),
        new(
            "[SIMULACIÓN] Problema crítico de almacenamiento",
            "KINGSTON SA400S37480G: la unidad informa un fallo SMART actual. " +
            "Comprueba la copia de seguridad y planifica su sustitución inmediata.",
            ToolTipIcon.Error),
    ];
}
