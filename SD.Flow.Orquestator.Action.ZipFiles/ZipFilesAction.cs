using SD.Flow.Orquestator.Core;
using System.IO.Compression;

public class ZipFilesAction : IWorkflowAction
{
    public string Name => "ZipFiles";

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        // Parámetros
        string pattern = args.GetValueOrDefault("SearchPattern", "*.*");

        string sourceDir = PathHelper.ResolveDynamicPath(args["SourceDirectory"]);
        string zipFilePath = PathHelper.ResolveDynamicPath(args["ZipFilePath"]);

        bool overwriteZip = bool.Parse(args.GetValueOrDefault("OverwriteZip", "true"));

        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Origen no encontrado: {sourceDir}");

        // Asegurar que la carpeta destino del ZIP existe
        string? zipDir = Path.GetDirectoryName(zipFilePath);
        if (!string.IsNullOrEmpty(zipDir) && !Directory.Exists(zipDir))
            Directory.CreateDirectory(zipDir);

        // Si el ZIP ya existe y queremos sobrescribir, lo borramos primero
        if (File.Exists(zipFilePath) && overwriteZip)
        {
            File.Delete(zipFilePath);
        }

        string[] filesToZip = Directory.GetFiles(sourceDir, pattern);

        if (filesToZip.Length == 0)
        {
            Console.WriteLine($"[Zip] No se encontraron archivos con el patrón '{pattern}' en {sourceDir}");
            return;
        }

        Console.WriteLine($"[Zip] Comprimiendo {filesToZip.Length} archivos en: {zipFilePath}...");

        using (ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Update))
        {
            foreach (var file in filesToZip)
            {
                string fileName = Path.GetFileName(file);
                // Evitar comprimir el propio archivo ZIP si está en la misma carpeta
                if (file == Path.GetFullPath(zipFilePath)) continue;

                archive.CreateEntryFromFile(file, fileName);
                Console.WriteLine($"      Añadido: {fileName}");
            }
        }

        Console.WriteLine("[Zip] Proceso de compresión completado.");
        await Task.CompletedTask;
    }
}