# SD.Flow.Orquestator

Motor de **workflows en .NET 8** definidos por JSON. El ejecutable principal carga **plugins** (`SD.Flow.Orquestator.Action.*.dll`) desde la carpeta `Actions/`. Solo las DLL presentes activan su funcionalidad — modelo pensado para entregar al cliente el host + las acciones contratadas.

> **Retomar el proyecto:** lee esta sección → [Inicio rápido](#inicio-rápido) → [Despliegue](#despliegue-y-carpetas) → [Motor de errores](#comportamiento-del-motor) → la acción que vayas a tocar.

---

## Inicio rápido

```bash
# Requisitos: .NET 8 SDK, Node.js (solo para scripts npm), Windows

npm run build              # Compila solución completa (host + Core + Actions)
npm run start              # Ejecuta leyendo SD.Flow.Orquestator/Input/workflow.json

npm run publish            # Build + limpieza + carpeta ./publish lista para copiar al cliente
```

**Archivo de flujo:** `SD.Flow.Orquestator/Input/workflow.json`  
**Solución Visual Studio:** `SD.Flow.Orquestator.sln`

---

## Concepto en una frase

| Pieza | Rol |
|-------|-----|
| **Host** (`SD.Flow.Orquestator.exe`) | Lee JSON, carga plugins, ejecuta pasos |
| **Core** (`SD.Flow.Orquestator.Core.dll`) | Motor, interfaz `IWorkflowAction`, logs, rutas dinámicas |
| **Actions/** | DLLs opcionales `*.Action.*.dll` (Copy, Move, Email, Shell, WhatsApp, …) |

El `ActionName` del JSON debe coincidir con `IWorkflowAction.Name` de la DLL cargada.

---

## Arquitectura

```
┌──────────────────────────────────────────────────────────┐
│  SD.Flow.Orquestator (Exe)                               │
│  Program.cs → LoadActions() → WorkflowEngine             │
│  Input/workflow.json                                     │
└─────────────────────────┬────────────────────────────────┘
                          │ ProjectReference
                          ▼
┌──────────────────────────────────────────────────────────┐
│  SD.Flow.Orquestator.Core                                │
│  IWorkflowAction, WorkflowEngine, PathHelper, Logger     │
└─────────────────────────┬────────────────────────────────┘
                          │ implementan (plugins)
                          ▼
┌──────────────────────────────────────────────────────────┐
│  Actions/SD.Flow.Orquestator.Action.*.dll                │
└──────────────────────────────────────────────────────────┘
```

### Flujo al ejecutar

1. `LoadActions()` escanea `Actions/*.Action.*.dll` e instancia clases que implementan `IWorkflowAction`.
2. Se deserializa `Input/workflow.json` → `WorkflowDefinition`.
3. `WorkflowEngine` recorre cada paso en orden y aplica las reglas de [errores](#comportamiento-del-motor).

---

## Proyectos de la solución

| Proyecto | Salida | `ActionName` |
|----------|--------|--------------|
| `SD.Flow.Orquestator` | Exe (raíz) | — |
| `SD.Flow.Orquestator.Core` | `Core/bin/Release/*.dll` → copiada junto al exe | — |
| `Action.CopyFile` | `Actions/*.dll` | `CopyFile` |
| `Action.CopyFiles` | `Actions/*.dll` | `CopyFiles` |
| `Action.MoveFile` | `Actions/*.dll` | `MoveFile` (un archivo) |
| `Action.MoveFiles` | `Actions/*.dll` | `MoveFiles` (lote + comodines) |
| `Action.DeleteFiles` | `Actions/*.dll` | `DeleteFiles` |
| `Action.ZipFiles` | `Actions/*.dll` | `ZipFiles` |
| `Action.SendEmail` | `Actions/*.dll` | `SendEmail` |
| `Action.Shell` | `Actions/*.dll` | `ExecuteShell` |
| `Action.WhatsApp` | `Actions/*.dll` | `WhatsApp` (+ Python) |

---

## Build del repositorio

### Archivos MSBuild importantes

| Archivo | Qué hace |
|---------|----------|
| `Directory.Build.props` | Los proyectos `Action.*` compilan directo a `SD.Flow.Orquestator/Actions/` |
| `Directory.Build.targets` | Los `Action.*` **no copian** `Core.dll` a `Actions/` (`Private=false` en referencias) |
| Host `.csproj` | Solo copia al output/publish: `Actions/SD.Flow.Orquestator.Action.*.dll` |

**Core** compila en `SD.Flow.Orquestator.Core/bin/Release/` y el host la lleva a la **raíz** del ejecutable vía `ProjectReference`.

### Scripts npm (`package.json`)

Requiere [Node.js](https://nodejs.org/) para invocar scripts; la app es .NET.

| Comando | Uso |
|---------|-----|
| `npm run build` | Compila toda la solución (Release) |
| `npm run build:debug` | Compila en Debug |
| `npm run build:actions` | Solo módulos `Action.*` |
| `npm run start` | Ejecuta el orquestador |
| `npm run start:build` | Build + start |
| `npm run publish` | **Build + clean:actions + publish → `./publish`** |
| `npm run clean:actions` | Quita `Core.dll` / restos viejos de `Actions/` |
| `npm run clean` | `dotnet clean` de la solución |
| `npm run whatsapp:install` | `pip install -r Scripts/requirements.txt` |
| `npm run whatsapp:validate` | Valida Python y archivos (sin enviar) |
| `npm run whatsapp:test` | Dry-run del script Python |

Scripts PowerShell en `scripts/`: `build-actions.ps1`, `clean-actions-folder.ps1`, `install-whatsapp-deps.ps1`, `validate-whatsapp.ps1`, `test-whatsapp-dry-run.ps1`.

---

## Despliegue y carpetas

### Layout correcto (runtime / `publish/`)

```
SD.Flow.Orquestator/
├── SD.Flow.Orquestator.exe
├── SD.Flow.Orquestator.Core.dll     ← SOLO en raíz (obligatoria)
├── Input/
│   └── workflow.json                ← flujo a ejecutar
├── Actions/
│   ├── SD.Flow.Orquestator.Action.CopyFiles.dll
│   ├── SD.Flow.Orquestator.Action.MoveFiles.dll
│   └── …                            ← solo *.Action.*.dll (plugins)
├── Scripts/
│   └── send_ws.py                   ← si se usa WhatsApp
├── Examples/                        ← fragmentos JSON de referencia
└── Logs/
    └── Log_yyyy-MM-dd.txt
```

### Regla de oro: Core.dll

| Ubicación | ¿Core.dll? |
|-----------|------------|
| **Raíz** (junto al `.exe`) | **Sí** — el host la carga al arrancar |
| **`Actions/`** | **No** — solo plugins `SD.Flow.Orquestator.Action.*.dll` |

Si ves `Actions/SD.Flow.Orquestator.Core.dll` es un residuo de builds antiguos → `npm run clean:actions`.

`npm run publish` deja `./publish` con esa estructura (compila todo, limpia `Actions/`, publica el host y copia solo DLL de acciones).

### Modelo comercial / modular

El cliente recibe el **exe + Core + carpetas base** y solo las DLL de acciones que contrate en `Actions/`. Si el JSON pide una acción sin DLL, el paso se omite (ver motor).

---

## Workflow (`Input/workflow.json`)

```json
{
  "WorkflowName": "Mi proceso",
  "Steps": [
    {
      "Description": "Paso legible en logs",
      "ActionName": "MoveFiles",
      "Parameters": {
        "SourceDirectory": "C:/Entrada",
        "DestinationDirectory": "C:/Salida",
        "SearchPattern": "*.pdf",
        "DestinationLayout": "PreserveStructure"
      }
    }
  ]
}
```

- `ActionName` = `Name` de la acción en código.
- Parámetros = `Dictionary<string,string>` (todos string en JSON).
- Ejemplos sueltos por acción: `SD.Flow.Orquestator/Examples/*.json` (integrar dentro de `Steps`).

### Rutas dinámicas (`PathHelper`)

En rutas: `YYYY`, `MM`, `DD` → fecha actual. Ejemplo: `C:/Backup/YYYY/MM/DD`.

---

## Acciones — referencia rápida

| ActionName | Para qué | Parámetros principales |
|------------|----------|------------------------|
| `CopyFile` | Un archivo | `Source`, `Destination`, `Overwrite`, `SimulationMode` |
| `CopyFiles` | Varios por patrón | `SourceDirectory`, `DestinationDirectory`, `SearchPattern`, `Overwrite` |
| `MoveFile` | **Un** archivo | `Source`, `Destination` (si destino es carpeta, conserva nombre), `Overwrite`, `SimulationMode` |
| `MoveFiles` | **Lote** `*.ext` | Ver [MoveFiles](#movefile-y-movefiles) |
| `DeleteFiles` | Borrar | `Path`, `SearchPattern`, `Recursive` |
| `ZipFiles` | ZIP | `SourceDirectory`, `ZipFilePath`, `SearchPattern`, `OverwriteZip` |
| `SendEmail` | SMTP (MailKit) | `To`, `Subject`, `Body`, `SmtpServer`, `SmtpPort`, `SmtpUser`, `SmtpPass`, `AttachmentPath` |
| `ExecuteShell` | CMD / PowerShell | `Command`, `ShellType`, `WorkingDirectory`, `Wait` |
| `WhatsApp` | WhatsApp Web vía Python | `Phone`, `Message`, `DryRun`, `TimeoutSeconds` — ver [WhatsApp](#whatsapp) |

Cada acción puede declarar `RequiredParameterKeys`; si faltan en el JSON, el paso se omite sin detener el flujo.

---

## MoveFile y MoveFiles

| Acción | Cuándo usarla |
|--------|----------------|
| **MoveFile** | Un archivo concreto: `Source` → `Destination` |
| **MoveFiles** | Muchos archivos: `SearchPattern` (`*.pdf`, `*.xlsx`, …), filtros, exclusiones |

### `DestinationLayout` (solo MoveFiles)

| Valor | Efecto |
|-------|--------|
| `PreserveStructure` (**default**) | Hereda carpetas: `origen/fol1/a.pdf` → `destino/fol1/a.pdf` |
| `Flat` | Todo en una carpeta: `destino/a.pdf`, `destino/b.pdf` (sin subcarpetas) |

Alias: `PreserveFolderStructure` (`true`/`false`), `DestinoOrigen`, `DestinoNuevo`.

Otros parámetros útiles: `Recursive`, `ExcludedItems`, `DateFilter` (`2026-01-01 TO TODAY`, `>30d`), `DateProperty` (`Modified`/`Created`), `SimulationMode`, `Overwrite`.

---

## Comportamiento del motor

| Situación | Qué pasa |
|-----------|----------|
| DLL / acción no encontrada | Paso **omitido** + log; el flujo **sigue** |
| Parámetros obligatorios faltantes | Paso **omitido** + log; el flujo **sigue** |
| Excepción en un paso | Paso **falla**; pasos siguientes **omitidos** con mensaje “error en paso X” |
| DLL duplicada al cargar | Se usa la primera; advertencia en log |

Logs: consola + `Logs/Log_yyyy-MM-dd.txt` (`WorkflowLogger`).

---

## WhatsApp

Depende de **Python 3** + `pywhatkit` + `pynput` (no es API oficial de Meta).

```bash
npm run whatsapp:install    # pip install -r SD.Flow.Orquestator/Scripts/requirements.txt
npm run whatsapp:validate   # comprueba entorno
npm run whatsapp:test       # dry-run (no abre navegador)
```

En JSON:

```json
{
  "ActionName": "WhatsApp",
  "Parameters": {
    "Phone": "+58XXXXXXXXXX",
    "Message": "Texto",
    "DryRun": "false",
    "TimeoutSeconds": "180"
  }
}
```

- **`DryRun: true`** — simula sin enviar.
- La acción C# **espera** al proceso Python, captura stdout/stderr y falla si exit code ≠ 0.
- Envío real: sesión activa en [web.whatsapp.com](https://web.whatsapp.com), escritorio con navegador visible.

Códigos de salida del script: `0` ok, `1` error, `2` dependencia faltante.

---

## Crear un nuevo módulo Action

1. Proyecto biblioteca .NET 8: `SD.Flow.Orquestator.Action.MiAccion`.
2. `ProjectReference` a `SD.Flow.Orquestator.Core` (hereda `Directory.Build.*` → salida en `Actions/`).
3. Implementar `IWorkflowAction`:

```csharp
using SD.Flow.Orquestator.Core;

public class MiAccionAction : IWorkflowAction
{
    public string Name => "MiAccion";

    public IReadOnlyCollection<string> RequiredParameterKeys =>
        new[] { "ParametroObligatorio" };

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        // ...
        await Task.CompletedTask;
    }
}
```

4. El ensamblado **debe** llamarse `SD.Flow.Orquestator.Action.MiAccion.dll` (patrón `*.Action.*.dll`).
5. Agregar el proyecto a `SD.Flow.Orquestator.sln` y compilar con `npm run build`.

---

## Mapa del repositorio

```
SD.Flow.Orquestator/
├── SD.Flow.Orquestator.sln
├── package.json                 # scripts npm
├── Directory.Build.props        # OutDir Actions/ para plugins
├── Directory.Build.targets      # no copiar Core a Actions/
├── README.md
├── scripts/                     # PowerShell auxiliares
├── publish/                     # salida de npm run publish
├── SD.Flow.Orquestator/         # host + Input + Actions + Scripts + Examples
├── SD.Flow.Orquestator.Core/
└── SD.Flow.Orquestator.Action.*/
```

---

## Problemas frecuentes

| Síntoma | Causa probable | Qué hacer |
|---------|----------------|-----------|
| Acción no se ejecuta | Falta DLL en `Actions/` o `ActionName` distinto al `Name` | Revisar log al iniciar (“Acción registrada…”) |
| `Core.dll` en `Actions/` | Build viejo | `npm run clean:actions` |
| Publish sin todas las acciones | Solo se publicó el host | `npm run publish` (incluye build completo) |
| WhatsApp “lanzado” pero no envía | Python sin deps o sin sesión web | `whatsapp:install`, `whatsapp:validate`, login en WhatsApp Web |
| MoveFiles no respeta carpetas | `DestinationLayout` = `Flat` | Usar `PreserveStructure` |
| Paso omitido por parámetros | Falta clave en `RequiredParameterKeys` | Completar `Parameters` en JSON |

---

## Requisitos

- .NET 8 SDK  
- Windows (rutas, CMD, PowerShell)  
- Node.js 18+ (opcional, para scripts npm)  
- Python 3 + pip (solo acción WhatsApp)

---

## Notas técnicas

- **SendEmail** usa MailKit solo en su proyecto Action; credenciales SMTP pueden ir en JSON (`SmtpUser`, `SmtpPass`) o valores por defecto en código.
- **ExecuteShell** detecta `.ps1` / `.bat` vs comando inline; usa `ArgumentList` y captura salida si `Wait=true`.
- Los plugins resuelven **Core** desde el AppDomain del host (ya cargado en raíz); no necesitan `Core.dll` física en `Actions/`.
