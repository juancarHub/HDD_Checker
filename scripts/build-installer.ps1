[CmdletBinding()]
param(
    [string]$SmartmontoolsRoot = (Join-Path $env:ProgramFiles 'smartmontools'),
    [string]$InnoCompiler
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$packageRoot = Join-Path $projectRoot 'artifacts\package-win-x64'
$dependencyManifest = Import-PowerShellDataFile `
    (Join-Path $projectRoot 'installer\dependencies.psd1')
$smartctl = Join-Path $SmartmontoolsRoot 'bin\smartctl.exe'
$driveDatabase = Join-Path $SmartmontoolsRoot 'bin\drivedb.h'
$smartLicense = Join-Path $SmartmontoolsRoot 'doc\COPYING.txt'
foreach ($required in @($smartctl, $driveDatabase, $smartLicense)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "No se encontro el componente requerido: $required"
    }
}

$smartVersion = (& $smartctl --version | Select-Object -First 1) -join ''
if ($smartVersion -notmatch ('smartctl\s+' + [regex]::Escape(
        $dependencyManifest.Smartmontools.Version))) {
    throw "Version de smartmontools no admitida: $smartVersion"
}
foreach ($relativePath in $dependencyManifest.Smartmontools.Files.Keys) {
    $path = Join-Path $SmartmontoolsRoot $relativePath
    $actualHash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    $expectedHash = $dependencyManifest.Smartmontools.Files[$relativePath]
    if ($actualHash -ne $expectedHash) {
        throw "La huella no coincide para smartmontools $relativePath."
    }
}

& (Join-Path $PSScriptRoot 'build-package.ps1') -Runtime win-x64 -SelfContained

$bundledRoot = Join-Path $packageRoot 'tools\smartmontools'
New-Item -ItemType Directory -Path (Join-Path $bundledRoot 'bin') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $bundledRoot 'doc') -Force | Out-Null
Copy-Item -LiteralPath $smartctl -Destination (Join-Path $bundledRoot 'bin') -Force
Copy-Item -LiteralPath $driveDatabase -Destination (Join-Path $bundledRoot 'bin') -Force
Copy-Item -LiteralPath $smartLicense -Destination (Join-Path $bundledRoot 'doc') -Force
Copy-Item -LiteralPath (Join-Path $SmartmontoolsRoot 'doc\README.txt') `
    -Destination (Join-Path $bundledRoot 'doc') -Force

if ([string]::IsNullOrWhiteSpace($InnoCompiler)) {
    $candidates = @(
        (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $projectRoot '.tools\inno\ISCC.exe')
    )
    $InnoCompiler = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if ([string]::IsNullOrWhiteSpace($InnoCompiler) -or
    -not (Test-Path -LiteralPath $InnoCompiler)) {
    throw 'No se encontro ISCC.exe. Instala Inno Setup o indica -InnoCompiler.'
}
$innoVersion = (& $InnoCompiler --version) -join "`n"
if ($innoVersion -notmatch [regex]::Escape($dependencyManifest.InnoSetup.Version)) {
    throw "Version de Inno Setup no admitida: $innoVersion"
}

& $InnoCompiler (Join-Path $projectRoot 'installer\ST_Holmes.iss')
if ($LASTEXITCODE -ne 0) {
    throw 'No se pudo compilar el instalador de ST_Holmes.'
}

Write-Host "Instalador creado en: $(Join-Path $projectRoot 'artifacts\installer')"
