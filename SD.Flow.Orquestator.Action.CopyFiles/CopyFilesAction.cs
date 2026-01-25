using SD.Flow.Orquestator.Core;

public class CopyFilesAction : IWorkflowAction
{
    public string Name => "CopyFiles";

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        // Parámetros esperados
        string sourceDir = PathHelper.ResolveDynamicPath(args["SourceDirectory"]);
        string destDir = PathHelper.ResolveDynamicPath(args["DestinationDirectory"]);

        string pattern = args.GetValueOrDefault("SearchPattern", "*.*");
        bool overwrite = bool.Parse(args.GetValueOrDefault("Overwrite", "true"));

        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"No se encontró el origen: {sourceDir}");

        // Crear destino si no existe
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
            Console.WriteLine($"[Copy] Carpeta destino creada: {destDir}");
        }

        // Obtener lista de archivos
        string[] files = Directory.GetFiles(sourceDir, pattern);
        Console.WriteLine($"[Copy] Copiando {files.Length} archivos que coinciden con '{pattern}'...");

        int count = 0;
        foreach (string file in files)
        {
            try
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);

                File.Copy(file, destFile, overwrite);
                count++;
                Console.WriteLine($"      Copiado: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      Error al copiar {file}: {ex.Message}");
            }
        }

        Console.WriteLine($"[Copy] Finalizado. Se copiaron {count} archivos.");
        await Task.CompletedTask;
    }
}