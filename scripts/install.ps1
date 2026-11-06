[CmdletBinding()]
param(
    [string]$InstallRoot = (Join-Path $env:ProgramFiles 'HDD Checker'),
    [string]$DataRoot = (Join-Path $env:ProgramData 'HDD_Checker'),
    [string]$MigrateDataFrom
)

$ErrorActionPreference = 'Stop'
$serviceName = 'HddChecker'
$packageRoot = $PSScriptRoot
$principal = New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Ejecuta install.ps1 desde PowerShell como administrador.'
}

New-Item -ItemType Directory -Path $InstallRoot -Force | Out-Null
Get-ChildItem -LiteralPath $packageRoot -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $InstallRoot -Force
}
foreach ($directory in @('app', 'agent', 'service')) {
    $source = Join-Path $packageRoot $directory
    Copy-Item -LiteralPath $source -Destination $InstallRoot -Recurse -Force
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
}

& icacls.exe $DataRoot /grant '*S-1-5-32-545:(OI)(CI)RX' /T | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw 'No se pudieron establecer los permisos de lectura del catálogo.'
}

$serviceExecutable = Join-Path $InstallRoot 'service\HddChecker.Service.exe'
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if (-not $existingService) {
    New-Service -Name $serviceName -BinaryPathName ('"{0}"' -f $serviceExecutable) `
        -DisplayName 'HDD Checker' -StartupType Automatic `
        -Description 'Supervisión preventiva SMART y longitudinal de unidades de almacenamiento.' | Out-Null
} else {
    if ($existingService.Status -ne 'Stopped') {
        Stop-Service -Name $serviceName -Force
    }
    & sc.exe config $serviceName ('binPath= "{0}"' -f $serviceExecutable) start= auto | Out-Null
}

& sc.exe config $serviceName start= delayed-auto | Out-Null
$launcher = Join-Path $InstallRoot 'HDD_Checker.exe'
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' `
    -Name 'HDD Checker' -Value ('"{0}" /agent' -f $launcher)
Start-Service -Name $serviceName
Start-Process -FilePath $launcher -ArgumentList '/agent'

Write-Host "HDD Checker instalado en: $InstallRoot"
Write-Host "Datos de supervisión en: $DataRoot"
