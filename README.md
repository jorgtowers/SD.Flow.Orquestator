# SD.Flow.Orquestator

Motor de workflows para **.NET 8** que ejecuta pasos definidos en JSON. El ejecutable principal descubre y carga **módulos de acción como DLL** desde una carpeta `Actions`; solo las acciones cuyas DLL estén presentes quedan disponibles en tiempo de ejecución.

## Arquitectura

```
┌─────────────────────────────────────────────────────────────┐
│  SD.Flow.Orquestator (Exe)                                  │
│  • Lee Input/workflow.json                                  │
│  • LoadActions → escanea Actions/*.Action.*.dll             │
│  • WorkflowEngine ejecuta pasos secuenciales                  │
└──────────────────────────┬──────────────────────────────────┘
                           │ referencia
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  SD.Flow.Orquestator.Core (biblioteca compartida)            │
│  IWorkflowAction, WorkflowEngine, PathHelper, WorkflowLogger  │
└──────────────────────────┬──────────────────────────────────┘
                           │ implementan
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  SD.Flow.Orquestator.Action.* (DLLs en carpeta Actions/)    │
│  CopyFile, CopyFiles, MoveFile, MoveFiles, DeleteFiles,     │
│  ZipFiles, SendEmail, ExecuteShell, WhatsApp, …             │
└─────────────────────────────────────────────────────────────┘
```

### Flujo de ejecución

1. Al iniciar, `SDActions.LoadActions()` busca en `{BaseDirectory}/Actions` archivos que coincidan con el patrón `*.Action.*.dll`.
2. Por cada DLL, carga el ensamblado con `Assembly.LoadFrom` e instancia clases concretas que implementen `IWorkflowAction`.
3. Las acciones se registran en un diccionario por `Name` (debe coincidir con `ActionName` en el JSON).
4. Se deserializa `Input/workflow.json` en `WorkflowDefinition` y `WorkflowEngine` ejecuta cada paso en orden; ante un error en un paso, el workflow se aborta.

## Estructura de la solución

| Proyecto | Tipo | Descripción |
|----------|------|-------------|
| **SD.Flow.Orquestator** | `Exe` | Host principal, carga de DLLs y punto de entrada |
| **SD.Flow.Orquestator.Core** | `Class Library` | Contratos, motor, utilidades (`PathHelper`, logging) |
| **SD.Flow.Orquestator.Action.CopyFile** | `DLL` | Copia un archivo (`CopyFile`) |
| **SD.Flow.Orquestator.Action.CopyFiles** | `DLL` | Copia archivos por patrón (`CopyFiles`) |
| **SD.Flow.Orquestator.Action.MoveFile** | `DLL` | Mueve un archivo (`MoveFile`) |
| **SD.Flow.Orquestator.Action.MoveFiles** | `DLL` | Mueve archivos con filtros de fecha y simulación (`MoveFiles`) |
| **SD.Flow.Orquestator.Action.DeleteFiles** | `DLL` | Elimina archivos o por patrón (`DeleteFiles`) |
| **SD.Flow.Orquestator.Action.ZipFiles** | `DLL` | Comprime archivos en ZIP (`ZipFiles`) |
| **SD.Flow.Orquestator.Action.SendEmail** | `DLL` | Envío SMTP vía MailKit (`SendEmail`) |
| **SD.Flow.Orquestator.Action.Shell** | `DLL` | Ejecuta CMD o PowerShell (`ExecuteShell`) |
| **SD.Flow.Orquestator.Action.WhatsApp** | `DLL` | Lanza script Python para WhatsApp Web (`WhatsApp`) |

Abrir la solución: `SD.Flow.Orquestator.sln`

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows (acciones de archivos y shell orientadas a rutas Windows)
- Para **WhatsApp**: Python 3, dependencias en `Scripts/requirements.txt`, y el script `Scripts/send_ws.py` copiado junto al ejecutable

## Compilación y despliegue

### Scripts npm (`package.json`)

Requiere [Node.js](https://nodejs.org/) instalado (solo para invocar los scripts; el proyecto sigue siendo .NET).

| Comando | Descripción |
|---------|-------------|
| `npm run restore` | Restaura paquetes NuGet de la solución |
| `npm run build` | Compila toda la solución (Release) |
| `npm run build:debug` | Compila en Debug |
| `npm run build:host` | Compila solo el ejecutable principal |
| `npm run build:core` | Compila solo la biblioteca Core |
| `npm run build:actions` | Compila todos los módulos Action.* (vía `scripts/build-actions.ps1`) |
| `npm run build:actions:debug` | Igual que anterior en Debug |
| `npm run start` | Ejecuta el orquestador (Release) |
| `npm run start:debug` | Ejecuta en Debug |
| `npm run start:build` | Compila y luego ejecuta |
| `npm run watch` | Ejecuta con recarga al cambiar código (Debug) |
| `npm run publish` | Compila la solución, limpia `Actions/` y publica en `./publish` |
| `npm run clean:actions` | Elimina `Core.dll` y restos obsoletos de la carpeta `Actions/` |
| `npm run clean` | Limpia artefactos de compilación |
| `npm run whatsapp:install` | Instala `pywhatkit` y `pynput` vía pip |
| `npm run whatsapp:validate` | Verifica Python, dependencias y archivos (sin enviar mensajes) |
| `npm run whatsapp:test` | Prueba `send_ws.py` en modo `--dry-run` (sin navegador) |
| `npm run validate:whatsapp` | Alias de `whatsapp:validate` |

```bash
npm run build
npm run whatsapp:install
npm run whatsapp:validate
npm run start
```

### Compilar todo (dotnet directo)

```bash
dotnet build SD.Flow.Orquestator.sln -c Release
```

Los proyectos `Action.*` y `Core` usan `Directory.Build.props` para compilar directamente en `SD.Flow.Orquestator/Actions/`.

### Estructura de carpetas en runtime

Junto al ejecutable (`SD.Flow.Orquestator.exe`):

```
SD.Flow.Orquestator/
├── SD.Flow.Orquestator.exe
├── SD.Flow.Orquestator.Core.dll          ← OBLIGATORIO aquí (raíz, junto al .exe)
├── Input/
│   └── workflow.json                     (definición del flujo — obligatorio)
├── Actions/
│   ├── SD.Flow.Orquestator.Action.CopyFiles.dll   ← solo plugins *.Action.*.dll
│   ├── SD.Flow.Orquestator.Action.SendEmail.dll
│   └── …                                 (solo las que el cliente necesite)
```

**`SD.Flow.Orquestator.Core.dll` no debe estar en `Actions/`.**  
- **Raíz:** Core es la biblioteca del motor; el host la carga al iniciar.  
- **`Actions/`:** solo módulos `SD.Flow.Orquestator.Action.*.dll`. El cargador ignora otros nombres, pero duplicar Core ahí confunde y puede dejar versiones desactualizadas.

Si tras compilar antigua configuración quedó `Actions/Core.dll`, ejecute: `npm run clean:actions`

`npm run publish` ejecuta `build` + limpieza + `dotnet publish` en `./publish` con la misma regla.
├── Scripts/
│   └── send_ws.py                        (requerido si se usa WhatsApp)
├── Examples/                             (ejemplos de pasos sueltos)
└── Logs/
    └── Log_yyyy-MM-dd.txt
```

**Modelo de licenciamiento / funcionalidad:** el cliente recibe el ejecutable base y copia en `Actions/` únicamente las DLL de las acciones contratadas. Si `workflow.json` referencia una acción sin DLL cargada, ese paso no se ejecutará (el motor no encuentra la acción registrada).

## Configuración del workflow (`Input/workflow.json`)

Formato principal:

```json
{
  "WorkflowName": "Nombre descriptivo del flujo",
  "Steps": [
    {
      "Description": "Texto para logs",
      "ActionName": "CopyFiles",
      "Parameters": {
        "SourceDirectory": "C:/origen",
        "DestinationDirectory": "C:/destino",
        "SearchPattern": "*.*",
        "Overwrite": "true"
      }
    }
  ]
}
```

`ActionName` debe coincidir exactamente con la propiedad `Name` de la acción implementada.

### Acciones disponibles y parámetros principales

| ActionName | Parámetros clave |
|------------|-------------------|
| `CopyFile` | `Source`, `Destination` |
| `CopyFiles` | `SourceDirectory`, `DestinationDirectory`, `SearchPattern`, `Overwrite` |
| `MoveFile` | `Source`, `Destination`, `Overwrite`, `SimulationMode` |
| `MoveFiles` | `SourceDirectory`, `DestinationDirectory`, `SearchPattern`, `DestinationLayout`, `Recursive`, `ExcludedItems`, `DateFilter`, `DateProperty`, `Overwrite`, `SimulationMode` |
| `DeleteFiles` | `Path`, `SearchPattern`, `Recursive` |
| `ZipFiles` | `SourceDirectory`, `ZipFilePath`, `SearchPattern`, `OverwriteZip` |
| `SendEmail` | `To`, `Subject`, `Body`, `SmtpServer`, `SmtpPort`, `SmtpUser`, `SmtpPass`, `AttachmentPath` |
| `ExecuteShell` | `Command`, `ShellType` (`CMD` / `POWERSHELL`), `WorkingDirectory`, `Wait` |
| `WhatsApp` | `Phone`, `Message`, `DryRun` (opcional), `TimeoutSeconds` (opcional, default 180) |

### Rutas dinámicas (`PathHelper`)

En rutas de parámetros se pueden usar placeholders reemplazados por la fecha actual:

- `YYYY` → año (ej. `2026`)
- `MM` → mes
- `DD` → día

Ejemplo: `C:/Backup/YYYY/MM/DD`

### `MoveFile` vs `MoveFiles`

| Acción | Uso |
|--------|-----|
| **MoveFile** | Un solo archivo (`Source` → `Destination`). Si `Destination` es carpeta, conserva el nombre del archivo. |
| **MoveFiles** | Lote con comodines (`*.pdf`, `*.xlsx`, …), filtros de fecha y exclusiones. |

### Modo de destino en `MoveFiles` (`DestinationLayout`)

| Valor | Comportamiento | Ejemplo |
|-------|----------------|---------|
| `PreserveStructure` (por defecto) | Replica el árbol de carpetas del origen bajo el destino. | Origen: `C:/In/fol1/a.pdf`, `C:/In/fol2/int1/b.pdf` → Destino: `C:/Out/fol1/a.pdf`, `C:/Out/fol2/int1/b.pdf` |
| `Flat` | Todos los archivos en la carpeta destino, sin subcarpetas (solo nombre de archivo). | Los mismos archivos → `C:/Out/a.pdf`, `C:/Out/b.pdf` |

Alias aceptados: `PreserveFolderStructure` (`true`/`false`), `DestinoOrigen` (= PreserveStructure), `DestinoNuevo` (= Flat).

```json
"DestinationLayout": "PreserveStructure"
```

```json
"DestinationLayout": "Flat"
```

### Filtros de fecha en `MoveFiles`

- Rango: `"2026-01-01 TO TODAY"` o `"2026-01-01 TO 2026-01-15"`
- Relativo: `">30d"` (archivos con antigüedad mayor a 30 días en `Modified`/`Created`)

## Crear un nuevo módulo de acción

1. Crear proyecto de biblioteca .NET 8, por ejemplo `SD.Flow.Orquestator.Action.MiAccion`.
2. Referenciar `SD.Flow.Orquestator.Core`.
3. Implementar `IWorkflowAction`:

```csharp
using SD.Flow.Orquestator.Core;

public class MiAccionAction : IWorkflowAction
{
    public string Name => "MiAccion";

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        // leer args["Parametro"]
        await Task.CompletedTask;
    }
}
```

4. Configurar salida hacia `SD.Flow.Orquestator/Actions` (como el resto de proyectos `Action.*`).
5. El nombre del ensamblado debe contener `.Action.` para que el cargador lo detecte (ej. `SD.Flow.Orquestator.Action.MiAccion.dll`).
6. Copiar la DLL (y dependencias NuGet si las hay) a la carpeta `Actions` del despliegue.

## Ejemplos

En `SD.Flow.Orquestator/Examples/` hay fragmentos JSON por acción (`copyFiles.json`, `moveFiles.json`, `sendEmail.json`, etc.). Son **pasos individuales** de referencia; para ejecutar un flujo completo, integrarlos dentro de `Steps` en `workflow.json`.

## Comportamiento del motor ante errores

| Situación | Comportamiento |
|-----------|----------------|
| DLL / acción no encontrada | El paso se **omite** con log; el flujo **continúa**. |
| Parámetros obligatorios faltantes | El paso se **omite** con log; el flujo **continúa**. |
| Excepción durante un paso | Ese paso **falla**; los pasos siguientes se **omiten** con mensaje indicando el paso que falló. |
| Acción duplicada al cargar DLLs | Se conserva la primera; las demás se ignoran con advertencia. |

Parámetros obligatorios se declaran en cada acción mediante `RequiredParameterKeys` en `IWorkflowAction`.

## Logs

`WorkflowLogger` escribe en consola y en `Logs/Log_{fecha}.txt`.

## Acción WhatsApp

### Configuración inicial

```bash
npm run whatsapp:install
npm run whatsapp:validate
npm run whatsapp:test
```

### Parámetros en `workflow.json`

```json
{
  "Description": "Aviso por WhatsApp",
  "ActionName": "WhatsApp",
  "Parameters": {
    "Phone": "+584121234567",
    "Message": "Proceso finalizado correctamente.",
    "DryRun": "false",
    "TimeoutSeconds": "180"
  }
}
```

- **`DryRun`**: `"true"` simula el envío sin abrir el navegador (útil para pruebas).
- **`TimeoutSeconds`**: tiempo máximo de espera al script Python (por defecto 180 s).

### Captura de errores

La acción C# ahora:

1. Espera a que termine el proceso Python.
2. Registra **stdout** y **stderr** en los logs.
3. Lanza excepción si el código de salida es distinto de cero (el motor marcará el paso como fallido).
4. Detecta timeout si el script tarda más de lo configurado.

El script `send_ws.py` escribe errores en stderr y devuelve códigos de salida:

| Código | Significado |
|--------|-------------|
| `0` | Éxito |
| `1` | Error general o argumentos inválidos |
| `2` | Dependencia Python faltante (`pywhatkit`, `pynput`, etc.) |

### Requisitos de ejecución real

- Sesión activa en [web.whatsapp.com](https://web.whatsapp.com) en el navegador predeterminado.
- Escritorio interactivo (no apto para servicios Windows sin UI).

## Notas de desarrollo

- El host referencia `SD.Flow.Orquestator.Core` vía `ProjectReference` o DLL; los módulos deben compilarse contra la **misma versión** de Core que despliega el cliente.
- `MailKit` solo es necesario en el proyecto `Action.SendEmail`, no en el host (aunque el `.csproj` del host actualmente incluye el paquete sin uso directo).
- WhatsApp depende de automatización del navegador (`pywhatkit`); no es una API oficial de Meta. Use `DryRun: true` o `npm run whatsapp:test` para validar sin enviar mensajes.
