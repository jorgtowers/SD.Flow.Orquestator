using SD.Flow.Orquestator.Core;

public class MoveFilesAction : IWorkflowAction
{
    public string Name => "MoveFiles";

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        string sourceDir = Path.GetFullPath(PathHelper.ResolveDynamicPath(args["SourceDirectory"]));
        string destDir = Path.GetFullPath(PathHelper.ResolveDynamicPath(args["DestinationDirectory"]));
        string searchPattern = args.GetValueOrDefault("SearchPattern", "*.*");
        var excludedItems = args.GetValueOrDefault("ExcludedItems", "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).ToList();

        if (!Directory.Exists(sourceDir)) throw new DirectoryNotFoundException($"Origen no encontrado: {sourceDir}");

        // 1. Recopilar archivos
        string[] patterns = searchPattern.Split(',').Select(p => p.Trim()).ToArray();
        var allFiles = patterns.SelectMany(p =>
            Directory.GetFiles(sourceDir, p, SearchOption.AllDirectories)).ToList();

        // 2. Mover archivos manteniendo estructura
        foreach (var filePath in allFiles)
        {
            string relativePath = Path.GetRelativePath(sourceDir, filePath);

            // Validar exclusión
            if (excludedItems.Any(ex => relativePath.Split(Path.DirectorySeparatorChar)
                .Any(part => part.Equals(ex, StringComparison.OrdinalIgnoreCase)))) continue;

            string finalDestPath = Path.Combine(destDir, relativePath);
            string? finalDestFolder = Path.GetDirectoryName(finalDestPath);

            if (!string.IsNullOrEmpty(finalDestFolder)) Directory.CreateDirectory(finalDestFolder);

            if (File.Exists(finalDestPath)) File.Delete(finalDestPath);
            File.Move(filePath, finalDestPath);
        }

        // 3. LIMPIEZA DE CARPETAS VACÍAS
        // Obtenemos todas las subcarpetas y las ordenamos por longitud (descendente)
        // para borrar primero las más profundas.
        var subFolders = Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length);

        foreach (var folder in subFolders)
        {
            // Solo borramos si no tiene archivos ni otras subcarpetas
            if (Directory.Exists(folder) &&
                !Directory.EnumerateFileSystemEntries(folder).Any())
            {
                try { Directory.Delete(folder); }
                catch { /* Evitar errores si una carpeta está bloqueada */ }
            }
        }

        Console.WriteLine("[MoveFiles] Movimiento y limpieza completados.");
        await Task.CompletedTask;
    }
}