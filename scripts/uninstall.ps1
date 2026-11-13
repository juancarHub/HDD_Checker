[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$InstallRoot = (Join-Path $env:ProgramFiles 'ST_Holmes'),
    [string]$DataRoot = (Join-Path $env:ProgramData 'ST_Holmes'),
    [switch]$RemoveData,
    [switch]$SkipFileRemoval
)

$ErrorActionPreference = 'Stop'
$serviceName = 'STHolmes'
$principal = New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Ejecuta uninstall.ps1 desde PowerShell como administrador.'
}

$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {
    if ($service.Status -ne 'Stopped') {
        Stop-Service -Name $serviceName -Force
    }
    & sc.exe delete $serviceName | Out-Null
}

Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' `
    -Name 'ST_Holmes' -ErrorAction SilentlyContinue
$startMenuShortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'ST_Holmes.lnk'
Remove-Item -LiteralPath $startMenuShortcut -Force -ErrorAction SilentlyContinue
Get-Process -Name 'STHolmes.Agent','STHolmes.App' -ErrorAction SilentlyContinue | `
    Stop-Process -Force
if (-not $SkipFileRemoval -and (Test-Path -LiteralPath $InstallRoot)) {
    Remove-Item -LiteralPath $InstallRoot -Recurse -Force
}

if ($RemoveData -and (Test-Path -LiteralPath $DataRoot)) {
    Remove-Item -LiteralPath $DataRoot -Recurse -Force
    Write-Host 'Tambien se eliminaron las capturas y el catalogo local.'
} else {
    Write-Host "Datos conservados en: $DataRoot"
}
