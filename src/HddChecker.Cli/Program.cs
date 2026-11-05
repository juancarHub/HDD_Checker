using HddChecker.Core.Smartctl;

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

    if (parsed.Command.Equals("inspect", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(parsed.Device))
        {
            Console.Error.WriteLine("Falta la ruta del dispositivo para inspect.");
            return 2;
        }

        var result = await runner.RunAsync(["--all", "--json=c", parsed.Device]);
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

static (string Command, string? Device, string SmartctlPath, bool ShowHelp) ParseArguments(string[] arguments)
{
    if (arguments.Length == 0 || arguments.Any(argument => argument is "-h" or "--help"))
    {
        return (string.Empty, null, DefaultSmartctlPath, true);
    }

    var smartctlPath = DefaultSmartctlPath;
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

        positional.Add(arguments[index]);
    }

    return (
        positional.ElementAtOrDefault(0) ?? string.Empty,
        positional.ElementAtOrDefault(1),
        smartctlPath,
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
    PrintField("Temperatura (°C)", snapshot.TemperatureCelsius);
    PrintField("Vida restante (%)", snapshot.RemainingLifePercent);
    PrintField("Sectores reasignados", snapshot.ReallocatedSectors);
    PrintField("Sectores pendientes", snapshot.PendingSectors);
    PrintField("Sectores irrecuperables", snapshot.OfflineUncorrectableSectors);
    PrintField("Errores no corregibles", snapshot.ReportedUncorrectableErrors);
    PrintField("Errores CRC SATA", snapshot.SataCrcErrors);
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

static void PrintHelp()
{
    Console.WriteLine("HDD_Checker acquisition laboratory");
    Console.WriteLine();
    Console.WriteLine("Uso:");
    Console.WriteLine("  hdd-checker scan [--smartctl <ruta>]");
    Console.WriteLine("  hdd-checker inspect <dispositivo> [--smartctl <ruta>]");
    Console.WriteLine();
    Console.WriteLine("Solo se realizan consultas de información. No se ejecutan autodiagnósticos ni escrituras.");
}
