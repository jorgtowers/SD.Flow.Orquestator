# Quita de Actions/ archivos que no son plugins (Core, PDB sueltos, proyectos obsoletos)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$actions = Join-Path $root "SD.Flow.Orquestator\Actions"

if (-not (Test-Path $actions)) {
    Write-Host "Carpeta Actions no existe, nada que limpiar."
    exit 0
}

$patterns = @(
    "SD.Flow.Orquestator.Core.*",
    "*MoveFille*"
)

$removed = 0
foreach ($pattern in $patterns) {
    Get-ChildItem -Path $actions -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "Eliminando: $($_.Name)" -ForegroundColor Yellow
        Remove-Item $_.FullName -Force
        $removed++
    }
}

Write-Host "Limpieza completada ($removed archivo(s) eliminados)." -ForegroundColor Green
