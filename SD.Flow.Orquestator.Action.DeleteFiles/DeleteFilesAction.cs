using SD.Flow.Orquestator.Core;

public class DeleteFilesAction : IWorkflowAction
{
    public string Name => "DeleteFiles";

    public IReadOnlyCollection<string> RequiredParameterKeys => new[] { "Path" };

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        string targetPath = PathHelper.ResolveDynamicPath(args["Path"]);
        string pattern = args.GetValueOrDefault("SearchPattern", "");
        bool recursive = bool.Parse(args.GetValueOrDefault("Recursive", "false"));

        if (File.Exists(targetPath) && string.IsNullOrEmpty(pattern))
        {
            File.Delete(targetPath);
            WorkflowLogger.Log($"[Delete] Archivo eliminado: {targetPath}");
        }
        else if (Directory.Exists(targetPath))
        {
            string searchPattern = string.IsNullOrEmpty(pattern) ? "*.*" : pattern;
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            string[] files = Directory.GetFiles(targetPath, searchPattern, option);

            WorkflowLogger.Log($"[Delete] Encontrados {files.Length} archivos para eliminar en {targetPath}.");

            foreach (var file in files)
            {
                File.Delete(file);
                WorkflowLogger.Log($"      Eliminado: {Path.GetFileName(file)}");
            }
        }
        else
        {
            WorkflowLogger.Log($"[Delete] No se encontró nada para borrar en: {targetPath}");
        }

        await Task.CompletedTask;
    }
}
