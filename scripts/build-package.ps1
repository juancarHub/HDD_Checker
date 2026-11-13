[CmdletBinding()]
param(
    [ValidateSet('win-x64')]
    [string]$Runtime = 'win-x64',
    [switch]$SelfContained
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$packageRoot = Join-Path $projectRoot "artifacts\package-$Runtime"

if (Test-Path -LiteralPath $packageRoot) {
    Get-ChildItem -LiteralPath $packageRoot -Force | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $packageRoot | Out-Null
}

$projects = @(
    @{ Project = 'src\HddChecker.Launcher'; Output = '.' },
    @{ Project = 'src\HddChecker.App'; Output = 'app' },
    @{ Project = 'src\HddChecker.Agent'; Output = 'agent' },
    @{ Project = 'src\HddChecker.Service'; Output = 'service' }
)

foreach ($item in $projects) {
    $projectPath = Join-Path $projectRoot $item.Project
    $outputPath = Join-Path $packageRoot $item.Output
    dotnet publish $projectPath --configuration Release --runtime $Runtime `
        --self-contained $SelfContained.IsPresent --output $outputPath
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo publicar $($item.Project)."
    }
}

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'install.ps1') -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'uninstall.ps1') -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $packageRoot
$manualPath = Join-Path $projectRoot 'output\pdf\ST_Holmes-Manual-de-usuario.pdf'
if (-not (Test-Path -LiteralPath $manualPath)) {
    throw 'Falta el manual de usuario. Ejecuta scripts\build-user-manual.py antes de crear el paquete.'
}
Copy-Item -LiteralPath $manualPath -Destination $packageRoot

Write-Host "Paquete creado en: $packageRoot"
