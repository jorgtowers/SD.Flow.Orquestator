using SD.Flow.Orquestator.Core;

namespace SD.Flow.Orquestator.Actions;

/// <summary>
/// Mueve un único archivo. Use MoveFiles para lotes con comodines (*.ext).
/// </summary>
public class MoveFileAction : IWorkflowAction
{
    public string Name => "MoveFile";

    public IReadOnlyCollection<string> RequiredParameterKeys => new[] { "Source", "Destination" };

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        string sourcePath = Path.GetFullPath(PathHelper.ResolveDynamicPath(args["Source"]));
        string destinationPath = PathHelper.ResolveDynamicPath(args["Destination"]);
        bool overwrite = bool.Parse(args.GetValueOrDefault("Overwrite", "true"));
        bool isSimulation = args.GetValueOrDefault("SimulationMode", "false")
            .Equals("true", StringComparison.OrdinalIgnoreCase);

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException($"El archivo origen no existe: {sourcePath}");

        destinationPath = ResolveDestinationFilePath(sourcePath, destinationPath);

        if (isSimulation)
        {
            WorkflowLogger.Log($"[SIMULACIÓN][MoveFile] Movería: {sourcePath} -> {destinationPath}");
            await Task.CompletedTask;
            return;
        }

        string? destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
            WorkflowLogger.Log($"[MoveFile] Directorio creado: {destinationDirectory}");
        }

        if (File.Exists(destinationPath) && !overwrite)
            throw new IOException($"El destino ya existe y Overwrite=false: {destinationPath}");

        if (File.Exists(destinationPath))
            File.Delete(destinationPath);

        File.Move(sourcePath, destinationPath);

        WorkflowLogger.Log("[MoveFile] Archivo movido con éxito:");
        WorkflowLogger.Log($"      Desde: {sourcePath}");
        WorkflowLogger.Log($"      Hacia: {destinationPath}");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Si Destination es carpeta (existente o termina en separador), usa el nombre del archivo origen.
    /// </summary>
    private static string ResolveDestinationFilePath(string sourcePath, string destinationPath)
    {
        bool looksLikeDirectory = destinationPath.EndsWith(Path.DirectorySeparatorChar) ||
                                  destinationPath.EndsWith('/') ||
                                  destinationPath.EndsWith('\\');

        if (looksLikeDirectory || Directory.Exists(destinationPath))
            return Path.GetFullPath(Path.Combine(destinationPath, Path.GetFileName(sourcePath)));

        return Path.GetFullPath(destinationPath);
    }
}
