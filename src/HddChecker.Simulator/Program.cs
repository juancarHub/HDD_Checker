using HddChecker.Core.Acquisition;
using HddChecker.Core.Smartctl;
using HddChecker.Storage;

var severity = args.FirstOrDefault()?.ToLowerInvariant() ?? "warning";
if (severity is not ("attention" or "warning" or "critical"))
{
    Console.Error.WriteLine("Uso: STHolmes.Simulator [attention|warning|critical]");
    return 2;
}

var simulationRoot = Path.GetFullPath(Path.Combine(
    "artifacts",
    $"simulation-{severity}-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}"));
var capturesRoot = Path.Combine(simulationRoot, "captures");
var databasePath = Path.Combine(simulationRoot, "data", "st-holmes.db");
var firstTimestamp = DateTimeOffset.UtcNow.AddDays(-1);
var secondTimestamp = DateTimeOffset.UtcNow;

await WriteCaptureAsync(capturesRoot, severity, firstTimestamp, isCurrent: false);
await WriteCaptureAsync(capturesRoot, severity, secondTimestamp, isCurrent: true);
var catalog = await CaptureCatalog.LoadAsync(capturesRoot);
var index = new CaptureIndex(databasePath);
await index.SynchronizeAsync(catalog);
var queued = await index.QueueLatestNotificationsAsync();
var notifications = await index.GetNotificationHistoryAsync();

Console.WriteLine("Simulación aislada preparada");
Console.WriteLine("-----------------------------");
Console.WriteLine($"Nivel: {severity}");
Console.WriteLine($"Carpeta: {simulationRoot}");
Console.WriteLine($"Unidades: {catalog.Devices.Count}");
Console.WriteLine($"Capturas: {catalog.Devices.Sum(device => device.Captures.Count)}");
Console.WriteLine($"Alertas nuevas: {queued}");
Console.WriteLine($"Incidencias históricas: {notifications.Count}");
Console.WriteLine();
Console.WriteLine("Esta simulación no contiene datos de discos reales.");
Console.WriteLine("Usa la carpeta indicada como --data-root para el agente y el panel.");
return 0;

static async Task WriteCaptureAsync(
    string capturesRoot,
    string severity,
    DateTimeOffset timestamp,
    bool isCurrent)
{
    var smartPassed = severity == "critical" && isCurrent ? "false" : "true";
    var reallocated = severity == "attention" && isCurrent ? 1 : 0;
    var pending = severity == "warning" && isCurrent ? 2 : 0;
    var json = $$"""
    {
      "device": { "name": "/dev/simulated", "type": "ata", "protocol": "ATA" },
      "model_name": "HDD CHECKER SIMULATED TEST DEVICE",
      "serial_number": "SIMULATION-{{severity.ToUpperInvariant()}}-001",
      "user_capacity": { "bytes": 500000000000 },
      "smart_status": { "passed": {{smartPassed}} },
      "power_on_time": { "hours": {{(isCurrent ? 101 : 100)}} },
      "temperature": { "current": {{(isCurrent ? 32 : 30)}} },
      "ata_smart_attributes": {
        "table": [
          { "id": 5, "name": "Reallocated_Sector_Ct", "value": 100,
            "worst": 100, "thresh": 10, "raw": { "value": {{reallocated}}, "string": "{{reallocated}}" } },
          { "id": 197, "name": "Current_Pending_Sector", "value": 100,
            "worst": 100, "thresh": 0, "raw": { "value": {{pending}}, "string": "{{pending}}" } }
        ]
      }
    }
    """;
    var exitCode = severity == "critical" && isCurrent ? 8 : 0;
    var command = new SmartctlCommandResult(
        "smartctl-simulator",
        ["--simulation", severity],
        timestamp,
        timestamp.AddSeconds(1),
        exitCode,
        json,
        string.Empty);
    await new AcquisitionPackageWriter().WriteAsync(
        capturesRoot,
        command,
        SmartctlResultAssessment.FromExitCode(exitCode),
        SmartctlJsonParser.ParseSnapshot(json));
}
