using SD.Flow.Orquestator.Core;

public class DeleteFilesAction : IWorkflowAction
{
    public string Name => "DeleteFiles";

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        // Parámetros: 
        // 'Path' puede ser un archivo directo o una carpeta.
        // 'SearchPattern' para borrar varios (ej: *.tmp).
        string targetPath = PathHelper.ResolveDynamicPath(args["Path"]);
        string pattern = args.GetValueOrDefault("SearchPattern", "");
        bool recursive = bool.Parse(args.GetValueOrDefault("Recursive", "false"));

        // Caso 1: Es un archivo individual
        if (File.Exists(targetPath) && string.IsNullOrEmpty(pattern))
        {
            File.Delete(targetPath);
            Console.WriteLine($"[Delete] Archivo eliminado: {targetPath}");
        }
        // Caso 2: Es una carpeta y queremos borrar archivos por patrón
        else if (Directory.Exists(targetPath))
        {
            string searchPattern = string.IsNullOrEmpty(pattern) ? "*.*" : pattern;
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            string[] files = Directory.GetFiles(targetPath, searchPattern, option);

            Console.WriteLine($"[Delete] Encontrados {files.Length} archivos para eliminar en {targetPath}.");

            foreach (var file in files)
            {
                File.Delete(file);
                Console.WriteLine($"      Eliminado: {Path.GetFileName(file)}");
            }
        }
        else
        {
            Console.WriteLine($"[Delete] No se encontró nada para borrar en: {targetPath}");
        }

        await Task.CompletedTask;
    }
}