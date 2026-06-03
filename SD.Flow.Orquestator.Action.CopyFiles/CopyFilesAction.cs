using SD.Flow.Orquestator.Core;

public class CopyFilesAction : IWorkflowAction
{
    public string Name => "CopyFiles";

    public IReadOnlyCollection<string> RequiredParameterKeys =>
        new[] { "SourceDirectory", "DestinationDirectory" };

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        string sourceDir = PathHelper.ResolveDynamicPath(args["SourceDirectory"]);
        string destDir = PathHelper.ResolveDynamicPath(args["DestinationDirectory"]);

        string pattern = args.GetValueOrDefault("SearchPattern", "*.*");
        bool overwrite = bool.Parse(args.GetValueOrDefault("Overwrite", "true"));

        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"No se encontró el origen: {sourceDir}");

        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
            WorkflowLogger.Log($"[Copy] Carpeta destino creada: {destDir}");
        }

        string[] files = Directory.GetFiles(sourceDir, pattern);
        WorkflowLogger.Log($"[Copy] Copiando {files.Length} archivos que coinciden con '{pattern}'...");

        int count = 0;
        foreach (string file in files)
        {
            try
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);

                File.Copy(file, destFile, overwrite);
                count++;
                WorkflowLogger.Log($"      Copiado: {fileName}");
            }
            catch (Exception ex)
            {
                WorkflowLogger.Log($"      Error al copiar {file}: {ex.Message}");
            }
        }

        WorkflowLogger.Log($"[Copy] Finalizado. Se copiaron {count} archivos.");
        await Task.CompletedTask;
    }
}
