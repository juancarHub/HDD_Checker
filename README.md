# HDD_Checker

Preventive SMART health monitoring for HDDs, SSDs and NVMe drives on Windows,
focused on longitudinal deterioration rather than a single exhaustive snapshot.

## Current stage

The project is in its acquisition-laboratory phase. The first application is a
manual, read-only console tool built around `smartctl` from smartmontools.

Initial goals:

- discover storage devices visible to `smartctl`;
- acquire structured JSON without writing to a device;
- preserve the original evidence and process result;
- normalize a small set of safe, comparable fields;
- distinguish zero, unavailable data and acquisition failures;
- learn which disks and USB adapters are compatible.

HDD_Checker does not predict an exact failure date and is not a substitute for
verified backups.

## Requirements

- Windows
- .NET 10 SDK
- smartmontools 7.5 or compatible

The default `smartctl` location is:

`C:\Program Files\smartmontools\bin\smartctl.exe`

Override it with `--smartctl <path>` when necessary.

## Usage

```powershell
dotnet run --project src/HddChecker.Cli -- scan
dotnet run --project src/HddChecker.Cli -- inspect /dev/sda
dotnet run --project src/HddChecker.Cli -- capture /dev/sda
dotnet run --project src/HddChecker.Cli -- capture /dev/sdd --device-type usbprolific
dotnet run --project src/HddChecker.Cli -- show captures/<capture-id>
dotnet run --project src/HddChecker.Cli -- compare captures/<older-id> captures/<newer-id>
dotnet run --project src/HddChecker.Cli -- history
dotnet run --project src/HddChecker.Cli -- trend OM8PDP
dotnet run --project src/HddChecker.Cli -- index
dotnet run --project src/HddChecker.Cli -- status
dotnet run --project src/HddChecker.Cli -- collect
```

`inspect` displays a normalized card without writing files. `capture` creates a
local evidence package containing:

- `smartctl.json`: the untouched JSON returned by smartctl;
- `acquisition.json`: command metadata, exit-status assessment and normalized
  snapshot;
- `smartctl.stderr.txt`: standard error, only when it is not empty.

Real acquisition packages remain in the ignored `captures/` directory. Choose a
different local directory with `--captures <path>`. Only explicitly anonymized
samples may be committed in the future.

`show` reopens an existing local package without querying the physical device.
`compare` verifies stable device identity, orders two captures by time and shows
raw metric deltas. Risk scoring and trend acceleration are deliberately deferred
until the acquisition and normalization layers have broader real-device coverage.

`history` scans the local evidence directory, reinterprets every raw JSON file
with the current normalizer and groups captures by WWN or serial number. Pass a
model or identity fragment to filter the catalog.

`trend` selects exactly one physical unit, orders all of its captures and shows
only metric changes between consecutive measurements. It reports ambiguity
instead of guessing when a filter matches multiple devices.

Trend findings use deterministic rules with explicit reasons. They distinguish
informational events, attention conditions, warnings and critical SMART changes;
they are not failure predictions.

Rates are calculated per power-on hour when that counter advances. Calendar-day
rates require intervals of at least 24 hours, preventing misleading extrapolation
from short laboratory measurements. Acceleration is deferred until two comparable
rates exist.

`index` synchronizes the local catalog into `data/hdd-checker.db`. The operation
is idempotent: repeated runs update normalized fields without duplicating devices
or captures. SQLite is a query index; the original JSON packages remain the
source evidence.

`status` reads the SQLite index without rescanning capture folders. It reports
the latest measurement per unit and the highest-severity finding attached to
the most recent interval. Run `index` after new captures before reading status.

`collect` executes one manual monitoring cycle: non-opening discovery, isolated
per-device acquisition, evidence persistence and SQLite synchronization. A
failed or timed-out unit is reported without preventing other devices from being
captured. Alternative paths that resolve to the same WWN or serial are reported
as duplicate routes and stored only once per cycle. This orchestration is
intended to be reused by the future service.

`HddChecker.Service` is the first unattended-monitoring host. It runs one cycle
when it starts and then waits for the configured interval before the next one;
cycles never overlap. Its `appsettings.json` controls the smartctl path, command
timeout, interval in minutes and optional data root. By default, service evidence
and the SQLite index live under `%ProgramData%\HDD_Checker`, separate from the
source tree and with no USB-specific assumptions. Service installation and
distribution packaging are deliberately deferred until this host has been
validated interactively.

`HddChecker.App` is the initial Windows desktop console. It reads the service's
SQLite index without requiring physical-drive privileges and presents the
current units, their explained current assessment and a per-device measurement
timeline with selectable metric charts. A single measurement is explicitly treated as insufficient for a
trend. Per-device incident history remains visible after its Windows notification
has been delivered. It is single-instance: a
second launch activates the existing window instead of creating another one.
During development, pass `--data-root <path>` to inspect an alternative local
catalog; the installed application will use the service data under ProgramData.

`HddChecker.Agent` is the per-user notification companion. It stays in the
Windows notification area, announces that supervision is active, polls the
persistent alert queue and displays each new attention, warning or critical
finding only once. Double-clicking its icon or choosing **Abrir panel** opens
the single-instance desktop console. Its **Probar avisos** submenu previews all
notification severities without writing incidents or altering monitoring data.
It never accesses physical drives.

`HDD_Checker.exe` is the installed entry point. With no arguments it ensures
the Windows service and notification agent are running; `/console` opens the
panel and `/agent` starts the notification companion. Development packaging and
installation instructions are documented in
[Windows installation](docs/installation.md).

See [Architecture](docs/architecture.md) for the current component boundaries
and persistence direction. See [Product vision](docs/product-vision.md) for the
longitudinal differentiator, honest limits and working-name status.

USB bridges may require the smartctl device type reported by `scan`. Pass it to
`inspect` or `capture` with `--device-type <type>`. Opening some physical drives
on Windows also requires an Administrator terminal.

## Safety

The laboratory currently performs information queries only. It does not launch
SMART self-tests, modify device settings or write data to storage devices.
Every smartctl process has a configurable timeout (30 seconds by default), so a
non-responsive device cannot block an acquisition cycle indefinitely.

## Tests

The automated suite uses synthetic, anonymized SMART JSON only. Real laboratory
captures and device identifiers remain outside Git.

```powershell
dotnet test HDD_Checker.slnx
```

## License

MIT
