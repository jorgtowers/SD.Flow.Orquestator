public static class WorkflowLogger
{
    private static readonly string LogFolder = "Logs";

    public static void Log(string message, string workflowName = "System")
    {
        if (!Directory.Exists(LogFolder)) Directory.CreateDirectory(LogFolder);

        string fileName = $"Log_{DateTime.Now:yyyy-MM-dd}.txt";
        string path = Path.Combine(LogFolder, fileName);

        string logEntry = $"[{DateTime.Now:HH:mm:ss}] [{workflowName}] {message}";

        // Imprimir en consola con color
        Console.WriteLine(logEntry);

        // Guardar en archivo
        File.AppendAllLines(path, new[] { logEntry });
    }

    public static void LogError(string message, string workflowName, Exception ex)
    {
        Log($"ERROR: {message} | Exception: {ex.Message}", workflowName);
    }
}