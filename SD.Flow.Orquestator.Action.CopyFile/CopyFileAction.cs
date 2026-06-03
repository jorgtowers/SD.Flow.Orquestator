using SD.Flow.Orquestator.Core;

namespace SD.Flow.Orquestator.Actions;

public class CopyFileAction : IWorkflowAction
{
    public string Name => "CopyFile";

    public IReadOnlyCollection<string> RequiredParameterKeys => new[] { "Source", "Destination" };

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        string sourceDir = PathHelper.ResolveDynamicPath(args["Source"]);
        string destDir = PathHelper.ResolveDynamicPath(args["Destination"]);
        File.Copy(sourceDir, destDir, true);
        WorkflowLogger.Log($"[Copy] Archivo copiado de {sourceDir} a {destDir}");
        await Task.CompletedTask;
    }
}
