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
```

Real acquisition files must remain in the ignored `captures/` directory. Only
explicitly anonymized samples may be committed in the future.

## Safety

The laboratory currently performs information queries only. It does not launch
SMART self-tests, modify device settings or write data to storage devices.

## License

MIT
