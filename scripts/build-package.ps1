[CmdletBinding()]
param(
    [ValidateSet('win-x64')]
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$packageRoot = Join-Path $projectRoot "artifacts\package-$Runtime"

if (Test-Path -LiteralPath $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $packageRoot | Out-Null

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
        --self-contained false --output $outputPath
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudo publicar $($item.Project)."
    }
}

Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'install.ps1') -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'uninstall.ps1') -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $packageRoot

Write-Host "Paquete creado en: $packageRoot"
