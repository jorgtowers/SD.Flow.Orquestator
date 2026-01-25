using SD.Flow.Orquestator.Core;
using System.Diagnostics;

public class WhatsAppAction : IWorkflowAction
{
    public string Name => "WhatsApp";

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        // Parámetros que vienen del JSON
        string phone = args["Phone"];
        string message = args["Message"];

        // Configuración interna (donde vive tu script de python)
        // Usamos AppContext.BaseDirectory para que busque el script en la carpeta del .exe
        string scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "send_ws.py");
        if (!File.Exists(scriptPath))
            throw new Exception($"No se encontró el script de Python en: {scriptPath}");

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "python",
            // Pasamos el script, el teléfono y el mensaje como argumentos
            Arguments = $"\"{scriptPath}\" {phone} \"{message}\"",
            UseShellExecute = true, // Para que pywhatkit pueda interactuar con el navegador
            CreateNoWindow = false
        };

        Console.WriteLine($"[WhatsApp] Enviando mensaje a {phone}...");

        try
        {
            // Lanzamos el proceso. No esperamos (Fire and Forget) 
            // porque pywhatkit tarda unos segundos en abrir el navegador.
            Process.Start(startInfo);
            Console.WriteLine("[WhatsApp] El script ha sido lanzado correctamente.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error al intentar ejecutar el script de WhatsApp: {ex.Message}");
        }

        await Task.CompletedTask;
    }
}