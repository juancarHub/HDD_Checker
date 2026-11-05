using HddChecker.Core.Acquisition;
using HddChecker.Core.Analysis;
using HddChecker.Core.Longitudinal;
using HddChecker.Core.Smartctl;
using HddChecker.Storage;

const string DefaultSmartctlPath = @"C:\Program Files\smartmontools\bin\smartctl.exe";

var parsed = ParseArguments(args);
if (parsed.ShowHelp)
{
    PrintHelp();
    return 0;
}

try
{
    var runner = new SmartctlRunner(parsed.SmartctlPath);

    if (parsed.Command.Equals("history", StringComparison.OrdinalIgnoreCase))
    {
        var catalog = await CaptureCatalog.LoadAsync(parsed.CapturesRoot);
        PrintCatalog(catalog, parsed.Device);
        return catalog.Issues.Count == 0 ? 0 : 1;
    }

    if (parsed.Command.Equals("index", StringComparison.OrdinalIgnoreCase))
    {
        var catalog = await CaptureCatalog.LoadAsync(parsed.CapturesRoot);
        var index = new CaptureIndex(parsed.DatabasePath);
        var summary = await index.SynchronizeAsync(catalog);
        var counts = await index.GetCountsAsync();
        Console.WriteLine("Índice SQLite actualizado");
        Console.WriteLine($"Base de datos: {index.DatabasePath}");
        Console.WriteLine($"Unidades indexadas: {counts.Devices}");
        Console.WriteLine($"Capturas indexadas: {counts.Captures}");
        Console.WriteLine($"Incidencias de catálogo: {summary.IssueCount}");
        return summary.IssueCount == 0 ? 0 : 1;
    }

    if (parsed.Command.Equals("status", StringComparison.OrdinalIgnoreCase))
    {
        var index = new CaptureIndex(parsed.DatabasePath);
        if (!File.Exists(index.DatabasePath))
        {
            Console.Error.WriteLine("El índice SQLite no existe. Ejecuta primero: hdd-checker index");
            return 2;
        }

        PrintStatuses(await index.GetStatusesAsync(parsed.Device), index.DatabasePath);
        return 0;
    }

    if (parsed.Command.Equals("trend", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(parsed.Device))
        {
            Console.Error.WriteLine("trend requiere un filtro de modelo o identidad.");
            return 2;
        }

        var catalog = await CaptureCatalog.LoadAsync(parsed.CapturesRoot);
        var matches = FilterDevices(catalog.Devices, parsed.Device);
        if (matches.Count == 0)
        {
            Console.Error.WriteLine($"No se encontró ninguna unidad para: {parsed.Device}");
            return 2;
        }

        if (matches.Count > 1)
        {
            Console.Error.WriteLine("El filtro coincide con varias unidades. Usa un modelo o identidad más específico:");
            foreach (var match in matches)
            {
                Console.Error.WriteLine($"- {match.Latest.Package.Snapshot.ModelName}: {match.Identity.Key}");
            }

            return 2;
        }

        PrintTrend(LongitudinalTrend.FromHistory(matches[0]));
        return 0;
    }

    if (parsed.Command.Equals("show", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(parsed.Device))
        {
            Console.Error.WriteLine("Falta la ruta de la captura para show.");
            return 2;
        }

        var package = await new AcquisitionPackageReader().ReadAsync(parsed.Device);
        PrintSnapshot(package.Snapshot);
        PrintAssessment(package.Assessment);
        Console.WriteLine();
        Console.WriteLine($"Captura: {package.CaptureId}");
        Console.WriteLine($"Fecha UTC: {package.CapturedAtUtc:O}");
        return 0;
    }

    if (parsed.Command.Equals("compare", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(parsed.Device) ||
            string.IsNullOrWhiteSpace(parsed.SecondPath))
        {
            Console.Error.WriteLine("compare requiere dos carpetas de captura.");
            return 2;
        }

        var reader = new AcquisitionPackageReader();
        var first = await reader.ReadAsync(parsed.Device);
        var second = await reader.ReadAsync(parsed.SecondPath);
        PrintComparison(LongitudinalComparer.Compare(first, second));
        return 0;
    }

    if (parsed.Command.Equals("scan", StringComparison.OrdinalIgnoreCase))
    {
        var result = await runner.RunAsync(["--scan-open", "--json=c"]);
        var devices = SmartctlJsonParser.ParseScan(result.StandardOutput);
        var assessment = SmartctlResultAssessment.FromExitCode(result.ExitCode);

        Console.WriteLine($"Dispositivos detectados: {devices.Count}");
        foreach (var device in devices)
        {
            Console.WriteLine($"- {device.Name} | tipo: {device.Type ?? "no disponible"} | protocolo: {device.Protocol ?? "no disponible"}");
        }

        PrintAssessment(assessment);
        return assessment.Outcome is AcquisitionOutcome.Fatal ? 1 : 0;
    }

    if (parsed.Command.Equals("inspect", StringComparison.OrdinalIgnoreCase) ||
        parsed.Command.Equals("capture", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(parsed.Device))
        {
            Console.Error.WriteLine("Falta la ruta del dispositivo para inspect.");
            return 2;
        }

        var inspectArguments = new List<string>();
        if (!string.IsNullOrWhiteSpace(parsed.DeviceType))
        {
            inspectArguments.Add("--device");
            inspectArguments.Add(parsed.DeviceType);
        }

        inspectArguments.Add("--all");
        inspectArguments.Add("--json=c");
        inspectArguments.Add(parsed.Device);

        var result = await runner.RunAsync(inspectArguments);
        var assessment = SmartctlResultAssessment.FromExitCode(result.ExitCode);
        if (assessment.Outcome is AcquisitionOutcome.Fatal)
        {
            Console.Error.WriteLine("No se pudo adquirir información SMART del dispositivo.");
            foreach (var message in SmartctlJsonParser.ParseMessages(result.StandardOutput))
            {
                Console.Error.WriteLine($"smartctl: {message}");
            }

            if (!string.IsNullOrWhiteSpace(result.StandardError))
            {
                Console.Error.WriteLine(result.StandardError);
            }

            PrintAssessment(assessment);
            return 1;
        }

        SmartDeviceSnapshot snapshot;
        try
        {
            snapshot = SmartctlJsonParser.ParseSnapshot(result.StandardOutput);
        }
        catch
        {
            Console.Error.WriteLine("smartctl no devolvió una adquisición JSON utilizable.");
            PrintAssessment(assessment);
            return 1;
        }

        PrintSnapshot(snapshot);
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            Console.Error.WriteLine(result.StandardError);
        }

        PrintAssessment(assessment);

        if (parsed.Command.Equals("capture", StringComparison.OrdinalIgnoreCase))
        {
            var writer = new AcquisitionPackageWriter();
            var captureDirectory = await writer.WriteAsync(
                parsed.CapturesRoot,
                result,
                assessment,
                snapshot);
            Console.WriteLine();
            Console.WriteLine($"Evidencia guardada localmente en: {captureDirectory}");
            Console.WriteLine("Esta carpeta está excluida de Git.");
        }

        return assessment.Outcome is AcquisitionOutcome.Fatal ? 1 : 0;
    }

    Console.Error.WriteLine($"Comando desconocido: {parsed.Command}");
    PrintHelp();
    return 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Error de adquisición: {exception.Message}");
    return 1;
}

static (
    string Command,
    string? Device,
    string? SecondPath,
    string? DeviceType,
    string SmartctlPath,
    string CapturesRoot,
    string DatabasePath,
    bool ShowHelp) ParseArguments(string[] arguments)
{
    if (arguments.Length == 0 || arguments.Any(argument => argument is "-h" or "--help"))
    {
        return (string.Empty, null, null, null, DefaultSmartctlPath, "captures", "data/hdd-checker.db", true);
    }

    var smartctlPath = DefaultSmartctlPath;
    var capturesRoot = "captures";
    var databasePath = "data/hdd-checker.db";
    string? deviceType = null;
    var positional = new List<string>();

    for (var index = 0; index < arguments.Length; index++)
    {
        if (arguments[index] == "--smartctl")
        {
            if (++index >= arguments.Length)
            {
                throw new ArgumentException("--smartctl requiere una ruta.");
            }

            smartctlPath = arguments[index];
            continue;
        }

        if (arguments[index] == "--captures")
        {
            if (++index >= arguments.Length)
            {
                throw new ArgumentException("--captures requiere una carpeta.");
            }

            capturesRoot = arguments[index];
            continue;
        }

        if (arguments[index] == "--device-type")
        {
            if (++index >= arguments.Length)
            {
                throw new ArgumentException("--device-type requiere un tipo de smartctl.");
            }

            deviceType = arguments[index];
            continue;
        }

        if (arguments[index] == "--database")
        {
            if (++index >= arguments.Length)
            {
                throw new ArgumentException("--database requiere una ruta.");
            }

            databasePath = arguments[index];
            continue;
        }

        positional.Add(arguments[index]);
    }

    return (
        positional.ElementAtOrDefault(0) ?? string.Empty,
        positional.ElementAtOrDefault(1),
        positional.ElementAtOrDefault(2),
        deviceType,
        smartctlPath,
        capturesRoot,
        databasePath,
        false);
}

static void PrintAssessment(SmartctlResultAssessment assessment)
{
    Console.WriteLine();
    Console.WriteLine($"Adquisición: {FormatOutcome(assessment.Outcome)}");
    Console.WriteLine($"smartctl: {(int)assessment.ExitStatus} ({assessment.ExitStatus})");
    Console.WriteLine($"Aviso de salud actual: {FormatBoolean(assessment.HasHealthWarning)}");
    Console.WriteLine($"Incidencia histórica registrada: {FormatBoolean(assessment.HasHistoricalWarning)}");
}

static void PrintSnapshot(SmartDeviceSnapshot snapshot)
{
    Console.WriteLine("Ficha de adquisición SMART");
    Console.WriteLine("---------------------------");
    PrintField("Dispositivo", snapshot.DeviceName);
    PrintField("Tipo", snapshot.DeviceType);
    PrintField("Protocolo", snapshot.Protocol);
    PrintField("Familia", snapshot.ModelFamily);
    PrintField("Modelo", snapshot.ModelName);
    PrintField("Serie", snapshot.SerialNumber);
    PrintField("WWN", snapshot.WorldWideName);
    PrintField("Capacidad (bytes)", snapshot.CapacityBytes);
    PrintField("SMART aprobado", snapshot.SmartPassed is null ? null : FormatBoolean(snapshot.SmartPassed.Value));
    PrintField("Horas de funcionamiento", snapshot.PowerOnHours);
    PrintField("Ciclos de encendido", snapshot.PowerCycleCount);
    PrintField("Ciclos de carga", snapshot.LoadCycleCount);
    PrintField("Temperatura (°C)", snapshot.TemperatureCelsius);
    PrintField("Vida restante (%)", snapshot.RemainingLifePercent);
    PrintField("Reserva NVMe disponible (%)", snapshot.NvmeAvailableSparePercent);
    PrintField("Unidades NVMe leídas", snapshot.NvmeDataUnitsRead);
    PrintField("Unidades NVMe escritas", snapshot.NvmeDataUnitsWritten);
    PrintField("Apagados inseguros NVMe", snapshot.NvmeUnsafeShutdowns);
    PrintField("Errores de medios NVMe", snapshot.NvmeMediaErrors);
    PrintField("Entradas de errores NVMe", snapshot.NvmeErrorLogEntries);
    PrintField("Sectores reasignados", snapshot.ReallocatedSectors);
    PrintField("Sectores pendientes", snapshot.PendingSectors);
    PrintField("Sectores irrecuperables", snapshot.OfflineUncorrectableSectors);
    PrintField("Errores no corregibles", snapshot.ReportedUncorrectableErrors);
    PrintField("Errores CRC SATA", snapshot.SataCrcErrors);

    if (snapshot.ThresholdEvents is { Count: > 0 })
    {
        Console.WriteLine("Atributos que alcanzaron el umbral:");
        foreach (var attribute in snapshot.ThresholdEvents)
        {
            var moment = attribute.WhenFailed.Equals("past", StringComparison.OrdinalIgnoreCase)
                ? "histórico"
                : "actual";
            Console.WriteLine(
                $"- {attribute.Id} {attribute.Name}: {moment}; " +
                $"valor {FormatValue(attribute.Value)}, peor {FormatValue(attribute.Worst)}, " +
                $"umbral {FormatValue(attribute.Threshold)}, RAW {attribute.RawValue ?? "no disponible"}");
        }
    }
}

static void PrintField(string label, object? value) =>
    Console.WriteLine($"{label}: {value ?? "no disponible"}");

static string FormatOutcome(AcquisitionOutcome outcome) => outcome switch
{
    AcquisitionOutcome.Complete => "completa",
    AcquisitionOutcome.Partial => "parcial (alguna función SMART no es compatible)",
    _ => "fallida",
};

static string FormatBoolean(bool value) => value ? "sí" : "no";

static string FormatValue(object? value) => value?.ToString() ?? "no disponible";

static void PrintComparison(LongitudinalComparison comparison)
{
    Console.WriteLine("Comparación longitudinal");
    Console.WriteLine("-----------------------");
    Console.WriteLine($"Identidad: {comparison.Identity.Key}");
    Console.WriteLine($"Medición anterior (UTC): {comparison.PreviousCaptureUtc:O}");
    Console.WriteLine($"Medición actual (UTC): {comparison.CurrentCaptureUtc:O}");
    Console.WriteLine($"Tiempo calendario: {comparison.CalendarElapsed}");
    Console.WriteLine($"Horas de uso transcurridas: {FormatValue(comparison.PowerOnHoursElapsed)}");
    Console.WriteLine();

    foreach (var metric in comparison.Metrics.Where(metric => metric.IsComparable))
    {
        var delta = metric.Delta >= 0 ? $"+{metric.Delta}" : metric.Delta.ToString();
        Console.WriteLine(
            $"{metric.Name}: {metric.Previous} → {metric.Current} " +
            $"(Δ {delta}{metric.Unit})");
    }

    Console.WriteLine();
    Console.WriteLine("Esta comparación describe cambios; todavía no asigna una puntuación de riesgo.");
}

static void PrintCatalog(CaptureCatalog catalog, string? identityFilter)
{
    var devices = string.IsNullOrWhiteSpace(identityFilter)
        ? catalog.Devices
        : FilterDevices(catalog.Devices, identityFilter);

    Console.WriteLine("Catálogo local de capturas");
    Console.WriteLine("--------------------------");
    Console.WriteLine($"Unidades: {devices.Count}");
    Console.WriteLine($"Capturas: {devices.Sum(device => device.Captures.Count)}");
    Console.WriteLine();

    foreach (var device in devices)
    {
        var snapshot = device.Latest.Package.Snapshot;
        Console.WriteLine(snapshot.ModelName ?? "Modelo no disponible");
        Console.WriteLine($"  Identidad: {device.Identity.Key}");
        Console.WriteLine($"  Protocolo: {snapshot.Protocol ?? "no disponible"}");
        Console.WriteLine($"  Capturas: {device.Captures.Count}");
        Console.WriteLine($"  Primera: {device.First.Package.CapturedAtUtc:O}");
        Console.WriteLine($"  Última: {device.Latest.Package.CapturedAtUtc:O}");
        Console.WriteLine($"  SMART aprobado: {FormatNullableBoolean(snapshot.SmartPassed)}");
        Console.WriteLine($"  Horas: {FormatValue(snapshot.PowerOnHours)}");
        Console.WriteLine($"  Vida restante: {FormatPercent(snapshot.RemainingLifePercent)}");
        Console.WriteLine();
    }

    if (catalog.Issues.Count > 0)
    {
        Console.WriteLine("Capturas que no pudieron indexarse:");
        foreach (var issue in catalog.Issues)
        {
            Console.WriteLine($"- {issue.Path}: {issue.Message}");
        }
    }
}

static IReadOnlyList<DeviceCaptureHistory> FilterDevices(
    IReadOnlyList<DeviceCaptureHistory> devices,
    string filter) =>
    devices
        .Where(device =>
            device.Identity.Key.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            (device.Latest.Package.Snapshot.ModelName?.Contains(
                filter,
                StringComparison.OrdinalIgnoreCase) ?? false))
        .ToArray();

static void PrintTrend(LongitudinalTrend trend)
{
    var snapshot = trend.Device.Latest.Package.Snapshot;
    Console.WriteLine("Trayectoria longitudinal");
    Console.WriteLine("-----------------------");
    Console.WriteLine($"Modelo: {snapshot.ModelName ?? "no disponible"}");
    Console.WriteLine($"Identidad: {trend.Device.Identity.Key}");
    Console.WriteLine($"Capturas: {trend.Device.Captures.Count}");
    Console.WriteLine($"Intervalos: {trend.Intervals.Count}");
    Console.WriteLine();

    if (trend.Intervals.Count == 0)
    {
        Console.WriteLine("Todavía no existe una segunda captura para calcular trayectoria.");
        return;
    }

    foreach (var interval in trend.Intervals)
    {
        var comparison = interval.Comparison;
        Console.WriteLine($"{comparison.PreviousCaptureUtc:O} → {comparison.CurrentCaptureUtc:O}");
        Console.WriteLine($"  Tiempo calendario: {comparison.CalendarElapsed}");

        var changes = comparison.Metrics.Where(metric => metric.HasChanged).ToArray();
        if (changes.Length == 0)
        {
            Console.WriteLine("  Sin cambios en los indicadores comparables.");
            Console.WriteLine();
            continue;
        }

        foreach (var metric in changes)
        {
            var delta = metric.Delta >= 0 ? $"+{metric.Delta}" : metric.Delta.ToString();
            Console.WriteLine(
                $"  {metric.Name}: {metric.Previous} → {metric.Current} " +
                $"(Δ {delta}{metric.Unit})");
        }

        foreach (var finding in interval.Findings)
        {
            Console.WriteLine($"  [{FormatSeverity(finding.Severity)}] {finding.Message}");
        }

        Console.WriteLine();
    }

    Console.WriteLine("Los hallazgos aplican reglas deterministas y explicables; no son una predicción de fallo.");
}

static string FormatSeverity(FindingSeverity severity) => severity switch
{
    FindingSeverity.Information => "informativo",
    FindingSeverity.Attention => "atención",
    FindingSeverity.Warning => "advertencia",
    FindingSeverity.Critical => "crítico",
    _ => severity.ToString(),
};

static void PrintStatuses(IReadOnlyList<DeviceStatus> statuses, string databasePath)
{
    Console.WriteLine("Estado actual de unidades");
    Console.WriteLine("-------------------------");
    Console.WriteLine($"Índice: {databasePath}");
    Console.WriteLine($"Unidades: {statuses.Count}");
    Console.WriteLine();

    foreach (var status in statuses)
    {
        Console.WriteLine(status.ModelName ?? "Modelo no disponible");
        Console.WriteLine($"  Identidad: {status.IdentityKey}");
        Console.WriteLine($"  Protocolo: {status.Protocol ?? "no disponible"}");
        Console.WriteLine($"  Capturas: {status.CaptureCount}");
        Console.WriteLine($"  Última medición (UTC): {status.LatestCaptureUtc:O}");
        Console.WriteLine($"  Adquisición: {FormatOutcomeText(status.AcquisitionOutcome)}");
        Console.WriteLine($"  SMART aprobado: {FormatNullableBoolean(status.SmartPassed)}");
        Console.WriteLine($"  Horas: {FormatValue(status.PowerOnHours)}");
        Console.WriteLine($"  Temperatura: {FormatTemperature(status.TemperatureCelsius)}");
        Console.WriteLine($"  Vida restante: {FormatPercent(status.RemainingLifePercent)}");
        Console.WriteLine($"  Reserva NVMe: {FormatPercent(status.NvmeAvailableSparePercent)}");
        Console.WriteLine($"  Reasignados: {FormatValue(status.ReallocatedSectors)}");
        Console.WriteLine($"  Pendientes: {FormatValue(status.PendingSectors)}");
        Console.WriteLine($"  Errores de medios NVMe: {FormatValue(status.NvmeMediaErrors)}");
        if (status.HighestFindingSeverity is not null)
        {
            Console.WriteLine(
                $"  Hallazgo reciente: [{FormatSeverity(status.HighestFindingSeverity.Value)}] " +
                status.HighestFindingMessage);
        }
        else
        {
            Console.WriteLine("  Hallazgo reciente: ninguno");
        }

        Console.WriteLine();
    }
}

static string FormatTemperature(int? value) => value is null ? "no disponible" : $"{value} °C";

static string FormatOutcomeText(string value) => value switch
{
    "Complete" => "completa",
    "Partial" => "parcial",
    "Fatal" => "fallida",
    _ => value,
};

static string FormatNullableBoolean(bool? value) => value switch
{
    true => "sí",
    false => "no",
    null => "no disponible",
};

static string FormatPercent(int? value) => value is null ? "no disponible" : $"{value} %";

static void PrintHelp()
{
    Console.WriteLine("HDD_Checker acquisition laboratory");
    Console.WriteLine();
    Console.WriteLine("Uso:");
    Console.WriteLine("  hdd-checker scan [--smartctl <ruta>]");
    Console.WriteLine("  hdd-checker inspect <dispositivo> [--device-type <tipo>] [--smartctl <ruta>]");
    Console.WriteLine("  hdd-checker capture <dispositivo> [--device-type <tipo>] [--captures <carpeta>] [--smartctl <ruta>]");
    Console.WriteLine("  hdd-checker show <carpeta-de-captura>");
    Console.WriteLine("  hdd-checker compare <captura-anterior> <captura-actual>");
    Console.WriteLine("  hdd-checker history [filtro] [--captures <carpeta>]");
    Console.WriteLine("  hdd-checker trend <modelo-o-identidad> [--captures <carpeta>]");
    Console.WriteLine("  hdd-checker index [--captures <carpeta>] [--database <archivo.db>]");
    Console.WriteLine("  hdd-checker status [filtro] [--database <archivo.db>]");
    Console.WriteLine();
    Console.WriteLine("Solo se realizan consultas de información. No se ejecutan autodiagnósticos ni escrituras.");
}
