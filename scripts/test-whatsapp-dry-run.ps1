# Prueba send_ws.py en modo simulación (no abre navegador ni envía mensajes)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $root "SD.Flow.Orquestator\Scripts\send_ws.py"

$phone = if ($args[0]) { $args[0] } else { "+580000000000" }
$message = if ($args[1]) { $args[1] } else { "Prueba dry-run desde npm" }

Write-Host ">> python send_ws.py --dry-run $phone" -ForegroundColor Cyan
python $scriptPath --dry-run $phone $message

if ($LASTEXITCODE -ne 0) {
    Write-Error "Dry-run falló con código $LASTEXITCODE"
}

Write-Host "[OK] Dry-run completado sin errores." -ForegroundColor Green
