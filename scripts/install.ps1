[CmdletBinding()]
param(
    [string]$InstallRoot = (Join-Path $env:ProgramFiles 'ST_Holmes'),
    [string]$DataRoot = (Join-Path $env:ProgramData 'ST_Holmes'),
    [string]$MigrateDataFrom,
    [switch]$SkipFileCopy
)

$ErrorActionPreference = 'Stop'
$serviceName = 'STHolmes'
$legacyServiceName = 'HddChecker'
$legacyInstallRoot = Join-Path $env:ProgramFiles 'HDD Checker'
$legacyDataRoot = Join-Path $env:ProgramData 'HDD_Checker'
$packageRoot = $PSScriptRoot
$principal = New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Ejecuta install.ps1 desde PowerShell como administrador.'
}

$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
$legacyService = Get-Service -Name $legacyServiceName -ErrorAction SilentlyContinue
if ($existingService -or $legacyService) {
    Write-Host 'Actualizando la instalacion existente a ST_Holmes...'
}
foreach ($service in @($existingService, $legacyService)) {
    if ($service -and $service.Status -ne 'Stopped') {
        Stop-Service -Name $service.Name -Force
        $service.WaitForStatus(
            [System.ServiceProcess.ServiceControllerStatus]::Stopped,
            [TimeSpan]::FromSeconds(20))
    }
}

Get-Process -Name 'STHolmes.Agent','STHolmes.App','HddChecker.Agent','HddChecker.App' `
    -ErrorAction SilentlyContinue | `
    Stop-Process -Force

New-Item -ItemType Directory -Path $InstallRoot -Force | Out-Null
if (-not $SkipFileCopy) {
    Get-ChildItem -LiteralPath $packageRoot -File | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $InstallRoot -Force
    }
    foreach ($directory in @('app', 'agent', 'service', 'tools')) {
        $source = Join-Path $packageRoot $directory
        if (Test-Path -LiteralPath $source) {
            Copy-Item -LiteralPath $source -Destination $InstallRoot -Recurse -Force
        }
    }
}

New-Item -ItemType Directory -Path (Join-Path $DataRoot 'captures') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $DataRoot 'data') -Force | Out-Null
if ($MigrateDataFrom) {
    $sourceRoot = (Resolve-Path -LiteralPath $MigrateDataFrom).Path
    foreach ($directory in @('captures', 'data')) {
        $source = Join-Path $sourceRoot $directory
        if (Test-Path -LiteralPath $source) {
            Copy-Item -LiteralPath $source -Destination $DataRoot -Recurse -Force
        }
    }
} elseif (-not (Test-Path -LiteralPath (Join-Path $DataRoot 'data\st-holmes.db')) -and
          (Test-Path -LiteralPath $legacyDataRoot)) {
    foreach ($directory in @('captures', 'data')) {
        $source = Join-Path $legacyDataRoot $directory
        if (Test-Path -LiteralPath $source) {
            Copy-Item -LiteralPath $source -Destination $DataRoot -Recurse -Force
        }
    }
}

$legacyDatabase = Join-Path $DataRoot 'data\hdd-checker.db'
$database = Join-Path $DataRoot 'data\st-holmes.db'
if (-not (Test-Path -LiteralPath $database) -and (Test-Path -LiteralPath $legacyDatabase)) {
    Copy-Item -LiteralPath $legacyDatabase -Destination $database
}

$legacyProfileRoot = Join-Path $env:LocalAppData 'HDD_Checker'
$profileRoot = Join-Path $env:LocalAppData 'ST_Holmes'
if (-not (Test-Path -LiteralPath $profileRoot) -and (Test-Path -LiteralPath $legacyProfileRoot)) {
    Copy-Item -LiteralPath $legacyProfileRoot -Destination $profileRoot -Recurse
}

& icacls.exe $DataRoot /grant '*S-1-5-32-545:(OI)(CI)RX' /T | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw 'No se pudieron establecer los permisos de lectura del catalogo.'
}

$serviceExecutable = Join-Path $InstallRoot 'service\STHolmes.Service.exe'
if ($legacyService) {
    & sc.exe delete $legacyServiceName | Out-Null
}
if (-not $existingService) {
    New-Service -Name $serviceName -BinaryPathName ('"{0}"' -f $serviceExecutable) `
        -DisplayName 'ST_Holmes' -StartupType Automatic `
        -Description 'Supervision preventiva SMART y longitudinal de unidades de almacenamiento.' | Out-Null
} else {
    if ($existingService.Status -ne 'Stopped') {
        Stop-Service -Name $serviceName -Force
    }
    & sc.exe config $serviceName ('binPath= "{0}"' -f $serviceExecutable) start= auto | Out-Null
}

& sc.exe config $serviceName start= delayed-auto | Out-Null
$launcher = Join-Path $InstallRoot 'ST_Holmes.exe'
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' `
    -Name 'HDD Checker' -ErrorAction SilentlyContinue
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' `
    -Name 'ST_Holmes' -Value ('"{0}" /agent' -f $launcher)
$legacyShortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'HDD Checker.lnk'
Remove-Item -LiteralPath $legacyShortcut -Force -ErrorAction SilentlyContinue
$startMenuShortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'ST_Holmes.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($startMenuShortcut)
$shortcut.TargetPath = $launcher
$shortcut.Arguments = '/console'
$shortcut.WorkingDirectory = $InstallRoot
$shortcut.Description = 'Abrir el panel de supervision ST_Holmes'
$shortcut.Save()
Start-Service -Name $serviceName
Start-Process -FilePath $launcher -ArgumentList '/agent'

if ($legacyInstallRoot -ne $InstallRoot -and (Test-Path -LiteralPath $legacyInstallRoot)) {
    Remove-Item -LiteralPath $legacyInstallRoot -Recurse -Force
}

Write-Host "ST_Holmes instalado en: $InstallRoot"
Write-Host "Datos de supervision en: $DataRoot"
