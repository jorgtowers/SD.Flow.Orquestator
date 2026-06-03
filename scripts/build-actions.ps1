# Compila todos los proyectos SD.Flow.Orquestator.Action.*
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$config = if ($args -contains "Debug") { "Debug" } else { "Release" }

$actionProjects = Get-ChildItem -Path $root -Directory -Filter "SD.Flow.Orquestator.Action.*" |
    ForEach-Object { Join-Path $_.FullName "$($_.Name).csproj" } |
    Where-Object { Test-Path $_ }

if (-not $actionProjects) {
    Write-Error "No se encontraron proyectos Action.* en $root"
}

foreach ($project in $actionProjects) {
    Write-Host ">> Compilando $(Split-Path -Leaf $project) [$config]..." -ForegroundColor Cyan
    dotnet build $project -c $config
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "DLL generadas en: $(Join-Path $root 'SD.Flow.Orquestator\Actions')" -ForegroundColor Green

& (Join-Path $PSScriptRoot "clean-actions-folder.ps1")
