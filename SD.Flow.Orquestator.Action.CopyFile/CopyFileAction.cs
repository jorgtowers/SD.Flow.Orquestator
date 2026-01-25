using SD.Flow.Orquestator.Core;

namespace FlowOrquestator.Actions
{
    public class CopyFileAction : IWorkflowAction
    {
        public string Name => "CopyFile";
        public async Task ExecuteAsync(Dictionary<string, string> args)
        {
            string sourceDir = PathHelper.ResolveDynamicPath(args["Source"]);
            string destDir = PathHelper.ResolveDynamicPath(args["Destination"]);
            File.Copy(sourceDir, destDir, true);
            Console.WriteLine($"[Copy] Archivo copiado de {sourceDir} a {destDir}");
            await Task.CompletedTask;
        }
    }
}
