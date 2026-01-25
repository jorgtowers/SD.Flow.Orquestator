using SD.Flow.Orquestator.Core;
using System.Globalization;

public class MoveFilesAction : IWorkflowAction
{

    public string Name => "MoveFiles";

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        // 1. Configuración de parámetros
        string sourceDir = Path.GetFullPath(PathHelper.ResolveDynamicPath(args["SourceDirectory"]));
        string destDir = Path.GetFullPath(PathHelper.ResolveDynamicPath(args["DestinationDirectory"]));
        string searchPattern = args.GetValueOrDefault("SearchPattern", "*.*");
        string dateFilter = args.GetValueOrDefault("DateFilter", string.Empty);
        string dateProperty = args.GetValueOrDefault("DateProperty", "Modified");

        // Parámetro de Simulación: Por seguridad, por defecto es 'false'
        bool isSimulation = args.GetValueOrDefault("SimulationMode", "false").Equals("true", StringComparison.OrdinalIgnoreCase);

        var excludedItems = args.GetValueOrDefault("ExcludedItems", "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (isSimulation) Console.WriteLine("--- MODO SIMULACIÓN ACTIVADO: No se realizarán cambios reales ---");

        if (!Directory.Exists(sourceDir)) throw new DirectoryNotFoundException(sourceDir);

        var patterns = searchPattern.Split(',', StringSplitOptions.TrimEntries);
        var allFiles = patterns.SelectMany(p => Directory.EnumerateFiles(sourceDir, p, SearchOption.AllDirectories));

        int filesProcessed = 0;

        foreach (var filePath in allFiles)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                DateTime fileDate = dateProperty.Equals("Created", StringComparison.OrdinalIgnoreCase) ? fileInfo.CreationTime : fileInfo.LastWriteTime;

                if (!IsDateMatch(fileDate, dateFilter)) continue;

                string relativePath = Path.GetRelativePath(sourceDir, filePath);
                if (excludedItems.Any(ex => relativePath.Contains(ex, StringComparison.OrdinalIgnoreCase))) continue;

                string finalDestPath = Path.Combine(destDir, relativePath);

                if (isSimulation)
                {
                    Console.WriteLine("[SIMULACIÓN] Movería: {From} -> {To}", relativePath, finalDestPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(finalDestPath)!);
                    if (File.Exists(finalDestPath)) File.Delete(finalDestPath);
                    File.Move(filePath, finalDestPath);
                }
                filesProcessed++;
            }
            catch (Exception ex) { Console.WriteLine($"Error en {ex.Message}"); }
        }

        if (!isSimulation)
        {
            CleanEmptyDirectories(sourceDir);
            Console.WriteLine("[MoveFiles] Se movieron {Count} archivos correctamente.", filesProcessed);
        }
        else
        {
            Console.WriteLine("[SIMULACIÓN] Se habrían movido {Count} archivos y se habría limpiado el origen.", filesProcessed);
        }

        await Task.CompletedTask;
    }

    private bool IsDateMatch(DateTime fileDate, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;

        if (filter.Contains(" TO ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = filter.Split(" TO ", StringSplitOptions.TrimEntries);
            return fileDate >= ParseDate(parts[0]) && fileDate <= ParseDate(parts[1]);
        }

        if (filter.StartsWith(">"))
        {
            string val = filter[1..].Trim();
            if (val.EndsWith("h")) return fileDate <= DateTime.Now.AddHours(-double.Parse(val.Replace("h", "")));
            if (val.EndsWith("d")) return fileDate <= DateTime.Now.AddDays(-double.Parse(val.Replace("d", "")));
            return fileDate >= ParseDate(val);
        }

        return true;
    }

    private DateTime ParseDate(string dateStr)
    {
        if (dateStr.Equals("TODAY", StringComparison.OrdinalIgnoreCase)) return DateTime.Today.AddDays(1).AddTicks(-1); // Fin del día de hoy
        if (dateStr.Equals("NOW", StringComparison.OrdinalIgnoreCase)) return DateTime.Now;
        return DateTime.ParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private void CleanEmptyDirectories(string root)
    {
        var folders = Directory.GetDirectories(root, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length);
        foreach (var folder in folders)
        {
            try { if (!Directory.EnumerateFileSystemEntries(folder).Any()) Directory.Delete(folder); }
            catch { }
        }
    }
}