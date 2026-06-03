# Validación local de la acción WhatsApp (sin enviar mensajes reales)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $root "SD.Flow.Orquestator\Scripts\send_ws.py"
$dllPath = Join-Path $root "SD.Flow.Orquestator\Actions\SD.Flow.Orquestator.Action.WhatsApp.dll"

Write-Host "=== Validación WhatsApp ===" -ForegroundColor Cyan

$ok = $true

function Test-Check($label, [bool]$pass) {
    if ($pass) { Write-Host "[OK] $label" -ForegroundColor Green }
    else { Write-Host "[FAIL] $label" -ForegroundColor Red; $script:ok = $false }
}

Test-Check "Script send_ws.py existe" (Test-Path $scriptPath)
Test-Check "DLL WhatsApp compilada" (Test-Path $dllPath)

try {
    $pyVer = python --version 2>&1
    Test-Check "Python disponible: $pyVer" $true
} catch {
    Test-Check "Python disponible" $false
}

python -c "import pywhatkit; import pynput" 2>$null
Test-Check "Paquetes pywhatkit y pynput instalados" ($LASTEXITCODE -eq 0)

python -m py_compile $scriptPath 2>$null
Test-Check "Sintaxis de send_ws.py válida" ($LASTEXITCODE -eq 0)

# Simula invocación sin argumentos (no abre navegador)
$out = python $scriptPath 2>&1
Test-Check "Script responde sin argumentos" ($out -match "Faltan argumentos")

if ($ok) {
    Write-Host "`nDependencias y archivos OK." -ForegroundColor Green
    Write-Host "Para probar envío real: inicie sesión en web.whatsapp.com en el navegador predeterminado y ejecute npm run start." -ForegroundColor Yellow
    Write-Host "ADVERTENCIA: eso enviará mensajes a los números definidos en Input/workflow.json." -ForegroundColor Yellow
} else {
    Write-Host "`nCorrija los fallos. Instale dependencias con:" -ForegroundColor Red
    Write-Host "  npm run whatsapp:install"
    exit 1
}
