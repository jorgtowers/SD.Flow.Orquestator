# Instala dependencias Python para la acción WhatsApp
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$requirements = Join-Path $root "SD.Flow.Orquestator\Scripts\requirements.txt"

if (-not (Test-Path $requirements)) {
    Write-Error "No se encontró: $requirements"
}

Write-Host "Instalando dependencias desde requirements.txt..." -ForegroundColor Cyan
python -m pip install -r $requirements

if ($LASTEXITCODE -ne 0) {
    Write-Error "pip install falló con código $LASTEXITCODE"
}

Write-Host "Dependencias instaladas. Ejecute: npm run validate:whatsapp" -ForegroundColor Green
