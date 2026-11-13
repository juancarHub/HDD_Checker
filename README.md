# ST_Holmes

**La trayectoria de tus unidades, bajo investigación.**

Preventive SMART health monitoring for HDDs, SSDs and NVMe drives on Windows,
focused on longitudinal deterioration rather than a single exhaustive snapshot.

## Current stage

ST_Holmes is a working Windows monitoring application with a privileged service,
notification agent, desktop panel, longitudinal SQLite catalog and a
self-contained installer. It uses `smartctl` from smartmontools as a separate
read-only acquisition component.

Core capabilities:

- discover storage devices visible to `smartctl`;
- acquire structured JSON without writing to a device;
- preserve the original evidence and process result;
- normalize a small set of safe, comparable fields;
- distinguish zero, unavailable data and acquisition failures;
- explain current health and longitudinal changes;
- notify users with proportionate, persistent incidents;
- ignore transient USB storage during automatic supervision.

ST_Holmes does not predict an exact failure date and is not a substitute for
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

`index` synchronizes the local catalog into `data/st-holmes.db`. The operation
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

Automatic cycles deliberately ignore USB and SNT/SAT bridge devices. External
drives are transient tools in this product and are never captured, indexed,
shown in the panel or used for notifications. Manual `inspect` and `capture`
remain available for explicit compatibility laboratory work.

`STHolmes.Service` is the unattended-monitoring host. It runs one cycle
when it starts and then waits for the configured interval before the next one;
cycles never overlap. Its `appsettings.json` controls the smartctl path, command
timeout, interval in minutes and optional data root. By default, service evidence
and the SQLite index live under `%ProgramData%\ST_Holmes`, separate from the
source tree and with no USB-specific assumptions. Service installation and
distribution packaging are deliberately deferred until this host has been
validated interactively.

`STHolmes.App` is the Windows desktop console. It reads the service's
SQLite index without requiring physical-drive privileges and presents the
current units, their explained current assessment and a per-device measurement
timeline with selectable metric charts. A single measurement is explicitly treated as insufficient for a
trend. Per-device incident history remains visible after its Windows notification
has been delivered. It is single-instance: a
second launch activates the existing window instead of creating another one.
During development, pass `--data-root <path>` to inspect an alternative local
catalog; the installed application will use the service data under ProgramData.

`STHolmes.Agent` is the per-user notification companion. It stays in the
Windows notification area, announces that supervision is active, polls the
persistent alert queue and displays each new attention, warning or critical
finding only once. Double-clicking its icon or choosing **Abrir panel** opens
the single-instance desktop console. Its **Probar avisos** submenu previews all
notification severities without writing incidents or altering monitoring data.
Native Windows notifications are complemented by an actionable ST_Holmes card.
The tray icon is green without current findings, amber for attention or warning,
and red for critical state. It never accesses physical drives.

`ST_Holmes.exe` is the installed entry point. With no arguments it ensures
the Windows service and notification agent are running; `/console` opens the
panel and `/agent` starts the notification companion. Development packaging and
installation instructions are documented in
[Windows installation](docs/installation.md).

`STHolmes.Simulator` creates an isolated synthetic two-capture history under
the ignored `artifacts/` directory and queues one attention, warning or critical
incident through the production analysis and persistence pipeline. It never
reads or modifies real monitoring data and is excluded from installation
packages.

See [Architecture](docs/architecture.md) for the current component boundaries
and persistence direction. See [Product vision](docs/product-vision.md) for the
longitudinal differentiator and honest limits. See
[Rebuilding ST_Holmes](docs/building.md) for the pinned, reproducible release
process.

The standalone [User manual](output/pdf/ST_Holmes-Manual-de-usuario.pdf) is
written for ordinary users and is distributed with the installer. Its editable
source is [docs/user-manual.md](docs/user-manual.md).

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
dotnet test ST_Holmes.slnx
```

## License

MIT
