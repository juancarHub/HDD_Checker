[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$InstallRoot = (Join-Path $env:ProgramFiles 'HDD Checker'),
    [string]$DataRoot = (Join-Path $env:ProgramData 'HDD_Checker'),
    [switch]$RemoveData
)

$ErrorActionPreference = 'Stop'
$serviceName = 'HddChecker'
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
    -Name 'HDD Checker' -ErrorAction SilentlyContinue
$startMenuShortcut = Join-Path ([Environment]::GetFolderPath('Programs')) 'HDD Checker.lnk'
Remove-Item -LiteralPath $startMenuShortcut -Force -ErrorAction SilentlyContinue
Get-Process -Name 'HddChecker.Agent','HddChecker.App' -ErrorAction SilentlyContinue | `
    Stop-Process -Force
if (Test-Path -LiteralPath $InstallRoot) {
    Remove-Item -LiteralPath $InstallRoot -Recurse -Force
}

if ($RemoveData -and (Test-Path -LiteralPath $DataRoot)) {
    Remove-Item -LiteralPath $DataRoot -Recurse -Force
    Write-Host 'Tambien se eliminaron las capturas y el catalogo local.'
} else {
    Write-Host "Datos conservados en: $DataRoot"
}
