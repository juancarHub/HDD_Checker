# Windows installation

The current installer is an explicit PowerShell development package, not yet a
signed public MSI. It installs four separate processes behind one entry point:

- `HDD_Checker.exe` starts or verifies supervision;
- `HDD_Checker.exe /console` opens the single-instance desktop panel;
- `HDD_Checker.exe /agent` starts the per-user notification agent;
- `HddChecker.Service.exe` runs privileged acquisition through Windows Service
  Control Manager.

Build a framework-dependent Windows package with:

```powershell
.\scripts\build-package.ps1
```

From an elevated PowerShell in `artifacts\package-win-x64`, install with:

```powershell
.\install.ps1
```

To preserve and migrate an existing development history:

```powershell
.\install.ps1 -MigrateDataFrom C:\path\to\HDD_Checker
```

The service starts automatically with a delayed start. The notification agent
starts for the current user at sign-in, and a Start menu shortcut opens the
single-instance console. Ordinary users receive read-only access
to `%ProgramData%\HDD_Checker`; notification delivery state stays under each
user's `%LocalAppData%\HDD_Checker` profile.

Running `install.ps1` again performs an in-place update: it stops the service,
panel and agent before replacing binaries, preserves ProgramData, then restarts
the service and notification agent.

Uninstallation preserves evidence and the SQLite catalog unless `-RemoveData`
is explicitly supplied:

```powershell
.\uninstall.ps1
```
