# Windows installation

For ordinary users, the public distribution is the self-contained
`ST_Holmes-Setup-<version>.exe`. It includes the .NET runtime and the required
smartmontools `smartctl` component, so no separate dependency installation is
needed. The bundled smartmontools files remain a separate GPL-licensed program
and include their original license notice.

The installer also places the independent user manual in the application
folder and adds a direct shortcut to it in the Start menu. The same PDF can be
distributed alongside the installer.

The current installer is an explicit PowerShell development package, not yet a
signed public MSI. It installs four separate processes behind one entry point:

- `ST_Holmes.exe` starts or verifies supervision;
- `ST_Holmes.exe /console` opens the single-instance desktop panel;
- `ST_Holmes.exe /agent` starts the per-user notification agent;
- `STHolmes.Service.exe` runs privileged acquisition through Windows Service
  Control Manager.

Build a framework-dependent Windows package with:

```powershell
.\scripts\build-package.ps1
```

Maintainers can build the public installer with Inno Setup and a local
smartmontools 7.5 installation:

```powershell
.\scripts\build-installer.ps1
```

Exact prerequisites, dependency hashes and the GitHub Release workflow are
documented in [Rebuilding ST_Holmes](building.md).

Ordinary operation, alert meanings, privacy, updates and troubleshooting are
documented in the separate [User manual](../output/pdf/ST_Holmes-Manual-de-usuario.pdf).

From an elevated PowerShell in `artifacts\package-win-x64`, install with:

```powershell
.\install.ps1
```

To preserve and migrate an existing development history:

```powershell
.\install.ps1 -MigrateDataFrom C:\path\to\ST_Holmes
```

The service starts automatically with a delayed start. The notification agent
starts for the current user at sign-in, and a Start menu shortcut opens the
single-instance console. Ordinary users receive read-only access
to `%ProgramData%\ST_Holmes`; notification delivery state stays under each
user's `%LocalAppData%\ST_Holmes` profile.

The installer automatically upgrades an existing HDD Checker installation. It
migrates its catalog, captures and delivered-notification state, replaces the
legacy service and startup entry, and leaves the old ProgramData copy intact as
a safety backup.

Running `install.ps1` again performs an in-place update: it stops the service,
panel and agent before replacing binaries, preserves ProgramData, then restarts
the service and notification agent.

Uninstallation preserves evidence and the SQLite catalog unless `-RemoveData`
is explicitly supplied:

```powershell
.\uninstall.ps1
```
