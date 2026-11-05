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

        Console.WriteLine($"Dispositivos detectados: {devices.Count}");
        foreach (var device in devices)
        {
            Console.WriteLine($"- {device.Name} | tipo: {device.Type ?? "no disponible"} | protocolo: {device.Protocol ?? "no disponible"}");
        }

        PrintExitStatus(result.ExitCode);
        return result.ExitCode == 0 ? 0 : 1;
    }

    if (parsed.Command.Equals("inspect", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(parsed.Device))
        {
            Console.Error.WriteLine("Falta la ruta del dispositivo para inspect.");
            return 2;
        }

        var result = await runner.RunAsync(["--all", "--json=c", parsed.Device]);
        Console.WriteLine(result.StandardOutput);
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            Console.Error.WriteLine(result.StandardError);
        }

        PrintExitStatus(result.ExitCode);
        return result.ExitCode == 0 ? 0 : 1;
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

static void PrintExitStatus(int exitCode)
{
    Console.WriteLine($"smartctl exit status: {exitCode} ({(SmartctlExitStatus)exitCode})");
}

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
