using SD.Flow.Orquestator.Core;
using System.Text.Json;

// --- 1. Registro de Servicios (Estrategias disponibles) ---
// Aquí añadimos todas las piezas que hemos construido
SDActions actions = new SDActions();
var availableActions = actions.LoadActions();

// --- 2. Preparación de Rutas y Archivo JSON ---
// Ajustamos la ruta para que busque en la carpeta 'input' junto al ejecutable
string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
string jsonPath = Path.Combine(baseDirectory, "Input", "workflow.json");

// Aseguramos que existan las carpetas básicas para evitar errores de inicio
if (!Directory.Exists(Path.Combine(baseDirectory, "Input")))
    Directory.CreateDirectory(Path.Combine(baseDirectory, "Input"));

if (!File.Exists(jsonPath))
{
    WorkflowLogger.Log($"Error: No se encontró el archivo de configuración en: {jsonPath}");
    Console.WriteLine("Por favor, crea el archivo workflow.json en la carpeta 'input'.");
    return;
}

// --- 3. Carga y Ejecución del Flujo ---
try
{
    WorkflowLogger.Log($"!============== {DateTime.Now:yyyy-MM-dd HH:mm} =================!");
    WorkflowLogger.Log("Leyendo configuración del flujo...");

    string jsonString = await File.ReadAllTextAsync(jsonPath);
    var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(jsonString, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    if (workflow == null || workflow.Steps == null)
    {
        WorkflowLogger.Log("Error: El JSON está vacío o tiene un formato inválido.");
        return;
    }

    // Instanciar el motor con nuestras acciones registradas
    var engine = new WorkflowEngine(availableActions);

    // Ejecutar el flujo de manera asíncrona
    await engine.RunWorkflowAsync(workflow);
}
catch (JsonException ex)
{
    WorkflowLogger.Log($"Error de formato en el JSON: {ex.Message}");
}
catch (Exception ex)
{
    WorkflowLogger.Log($"Error crítico durante la ejecución: {ex.Message}");
}

WorkflowLogger.Log("Presiona cualquier tecla para salir...");
//Console.ReadKey();