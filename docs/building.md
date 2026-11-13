# Rebuilding ST_Holmes and its installer

The repository contains everything needed to rebuild the applications, service,
user manual, development package and one-file public installer. Generated
binaries, real SMART captures and local databases are deliberately excluded.

Third-party build tools and the reviewed smartmontools binaries remain outside
Git. Their accepted versions and SHA-256 hashes are pinned in
`installer/dependencies.psd1`, so a different dependency cannot silently enter
a release.

## Requirements

- 64-bit Windows;
- .NET 10 SDK;
- smartmontools 7.5 installed under `C:\Program Files\smartmontools`;
- Inno Setup 7.1.1, or its `ISCC.exe` path passed explicitly;
- Python 3 with ReportLab when the PDF manual must be regenerated.

## Automated tests

```powershell
dotnet test ST_Holmes.slnx
```

Do not continue with a public build if any test fails.

## Rebuild the user manual

The editable source is `docs/user-manual.md`; its generator is
`scripts/build-user-manual.py`.

```powershell
python .\scripts\build-user-manual.py
```

The result is `output\pdf\ST_Holmes-Manual-de-usuario.pdf`. Render and inspect
the complete PDF after changing its text or layout. The packaging script stops
if this file is absent, ensuring that a release cannot omit its manual.

## Development package

```powershell
.\scripts\build-package.ps1
```

This creates the framework-dependent package under
`artifacts\package-win-x64`.

## Public installer

```powershell
.\scripts\build-icon.ps1
python .\scripts\build-user-manual.py
.\scripts\build-installer.ps1
```

If Inno Setup is not installed in a standard location:

```powershell
.\scripts\build-installer.ps1 `
  -InnoCompiler "C:\path\to\Inno Setup 7\ISCC.exe"
```

The result is `artifacts\installer\ST_Holmes-Setup-1.0.0.exe`. It is
self-contained, includes the .NET runtime, redistributes the pinned
smartmontools files with their GPL license, and installs the standalone manual
with a Start menu shortcut. The entire `artifacts` directory is ignored by Git.

The build validates dependencies, publishes every .NET application for
`win-x64`, assembles scripts and licenses, adds the manual and smartmontools,
and compiles `installer/ST_Holmes.iss`.

## Verify the release candidate

```powershell
Get-FileHash .\artifacts\installer\ST_Holmes-Setup-1.0.0.exe -Algorithm SHA256
```

On a 64-bit Windows test installation, confirm:

- clean installation and update over an existing installation;
- the `ST_Holmes` service runs and starts automatically;
- the notification icon and desktop panel open;
- an initial capture reaches `%ProgramData%\ST_Holmes`;
- the manual opens from the Start menu;
- uninstall removes the program while preserving monitoring history.

The public build is intentionally unsigned. Windows can therefore show Unknown
publisher or Microsoft Defender SmartScreen. Publish the SHA-256 beside every
release so users can verify the download.

## Publishing

Do not commit generated installers. Commit the source, installer definition,
dependency manifest and distributable PDF manual. Create a version tag such as
`v1.0.0`, create a GitHub Release from it, attach the installer and PDF manual,
and publish the installer SHA-256 plus the unsigned-build notice.
