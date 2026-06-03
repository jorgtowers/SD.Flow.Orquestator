using SD.Flow.Orquestator.Core;
using System.Globalization;

public class MoveFilesAction : IWorkflowAction
{
    public string Name => "MoveFiles";

    public IReadOnlyCollection<string> RequiredParameterKeys =>
        new[] { "SourceDirectory", "DestinationDirectory" };

    /// <summary>
    /// PreserveStructure: mantiene subcarpetas relativas al origen (fol1, fol2\int1, …).
    /// Flat: todos los archivos en la carpeta destino, solo por nombre de archivo.
    /// </summary>
    private enum DestinationLayoutMode
    {
        PreserveStructure,
        Flat
    }

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        string sourceDir = Path.GetFullPath(PathHelper.ResolveDynamicPath(args["SourceDirectory"]));
        string destDir = Path.GetFullPath(PathHelper.ResolveDynamicPath(args["DestinationDirectory"]));
        string searchPattern = args.GetValueOrDefault("SearchPattern", "*.*");
        string dateFilter = args.GetValueOrDefault("DateFilter", string.Empty);
        string dateProperty = args.GetValueOrDefault("DateProperty", "Modified");
        bool isSimulation = args.GetValueOrDefault("SimulationMode", "false")
            .Equals("true", StringComparison.OrdinalIgnoreCase);
        bool recursive = bool.Parse(args.GetValueOrDefault("Recursive", "true"));
        bool overwrite = bool.Parse(args.GetValueOrDefault("Overwrite", "true"));
        var layoutMode = ParseDestinationLayout(args);

        var excludedItems = args.GetValueOrDefault("ExcludedItems", "")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        if (isSimulation)
            WorkflowLogger.Log("--- MODO SIMULACIÓN ACTIVADO: No se realizarán cambios reales ---");

        WorkflowLogger.Log(layoutMode == DestinationLayoutMode.PreserveStructure
            ? "[MoveFiles] Modo destino: PreserveStructure (hereda carpetas del origen)."
            : "[MoveFiles] Modo destino: Flat (todos los archivos en una sola carpeta).");

        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException(sourceDir);

        if (!isSimulation)
            Directory.CreateDirectory(destDir);

        var patterns = searchPattern.Split(',', StringSplitOptions.TrimEntries);
        var allFiles = patterns.SelectMany(p => Directory.EnumerateFiles(sourceDir, p, searchOption)).Distinct();

        int filesProcessed = 0;
        int filesSkipped = 0;

        foreach (var filePath in allFiles)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                DateTime fileDate = dateProperty.Equals("Created", StringComparison.OrdinalIgnoreCase)
                    ? fileInfo.CreationTime
                    : fileInfo.LastWriteTime;

                if (!IsDateMatch(fileDate, dateFilter))
                    continue;

                string relativePath = Path.GetRelativePath(sourceDir, filePath);
                if (excludedItems.Any(ex => relativePath.Contains(ex, StringComparison.OrdinalIgnoreCase)))
                    continue;

                string finalDestPath = ResolveDestinationPath(
                    destDir, filePath, relativePath, layoutMode);

                if (isSimulation)
                {
                    WorkflowLogger.Log($"[SIMULACIÓN] Movería: {relativePath} -> {finalDestPath}");
                }
                else
                {
                    if (!MoveFileToDestination(filePath, finalDestPath, overwrite))
                    {
                        filesSkipped++;
                        WorkflowLogger.Log($"[MoveFiles] Omitido (ya existe): {Path.GetFileName(finalDestPath)}");
                        continue;
                    }
                }

                filesProcessed++;
            }
            catch (Exception ex)
            {
                WorkflowLogger.Log($"[MoveFiles] Error en '{filePath}': {ex.Message}");
            }
        }

        if (!isSimulation)
        {
            CleanEmptyDirectories(sourceDir);
            WorkflowLogger.Log(
                $"[MoveFiles] Finalizado. Movidos: {filesProcessed}, omitidos: {filesSkipped}.");
        }
        else
        {
            WorkflowLogger.Log(
                $"[SIMULACIÓN] Se habrían movido {filesProcessed} archivo(s); omitidos: {filesSkipped}.");
        }

        await Task.CompletedTask;
    }

    private static string ResolveDestinationPath(
        string destDir,
        string filePath,
        string relativePath,
        DestinationLayoutMode layoutMode)
    {
        return layoutMode switch
        {
            DestinationLayoutMode.PreserveStructure => Path.Combine(destDir, relativePath),
            DestinationLayoutMode.Flat => Path.Combine(destDir, Path.GetFileName(filePath)),
            _ => Path.Combine(destDir, relativePath)
        };
    }

    private static bool MoveFileToDestination(string sourcePath, string destinationPath, bool overwrite)
    {
        string? destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        if (File.Exists(destinationPath))
        {
            if (!overwrite)
                return false;
            File.Delete(destinationPath);
        }

        File.Move(sourcePath, destinationPath);
        return true;
    }

    private static DestinationLayoutMode ParseDestinationLayout(Dictionary<string, string> args)
    {
        if (args.TryGetValue("DestinationLayout", out string? layout) && !string.IsNullOrWhiteSpace(layout))
        {
            string normalized = layout.Trim();
            if (normalized.Equals("Flat", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("SingleFolder", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("AllTogether", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("DestinoNuevo", StringComparison.OrdinalIgnoreCase))
            {
                return DestinationLayoutMode.Flat;
            }

            if (normalized.Equals("PreserveStructure", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Mirror", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("KeepStructure", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("DestinoOrigen", StringComparison.OrdinalIgnoreCase))
            {
                return DestinationLayoutMode.PreserveStructure;
            }

            throw new ArgumentException(
                $"DestinationLayout '{layout}' no válido. Use PreserveStructure o Flat.");
        }

        if (args.TryGetValue("PreserveFolderStructure", out string? preserve))
        {
            return bool.Parse(preserve)
                ? DestinationLayoutMode.PreserveStructure
                : DestinationLayoutMode.Flat;
        }

        return DestinationLayoutMode.PreserveStructure;
    }

    private bool IsDateMatch(DateTime fileDate, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        if (filter.Contains(" TO ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = filter.Split(" TO ", StringSplitOptions.TrimEntries);
            return fileDate >= ParseDate(parts[0]) && fileDate <= ParseDate(parts[1]);
        }

        if (filter.StartsWith('>'))
        {
            string val = filter[1..].Trim();
            if (val.EndsWith('h'))
                return fileDate <= DateTime.Now.AddHours(-double.Parse(val[..^1]));
            if (val.EndsWith('d'))
                return fileDate <= DateTime.Now.AddDays(-double.Parse(val[..^1]));
            return fileDate >= ParseDate(val);
        }

        return true;
    }

    private DateTime ParseDate(string dateStr)
    {
        if (dateStr.Equals("TODAY", StringComparison.OrdinalIgnoreCase))
            return DateTime.Today.AddDays(1).AddTicks(-1);
        if (dateStr.Equals("NOW", StringComparison.OrdinalIgnoreCase))
            return DateTime.Now;
        return DateTime.ParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private void CleanEmptyDirectories(string root)
    {
        var folders = Directory.GetDirectories(root, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length);

        foreach (var folder in folders)
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(folder).Any())
                    Directory.Delete(folder);
            }
            catch
            {
                // Directorio en uso o sin permisos
            }
        }
    }
}
