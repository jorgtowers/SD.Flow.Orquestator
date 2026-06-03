using SD.Flow.Orquestator.Core;
using System.IO.Compression;

public class ZipFilesAction : IWorkflowAction
{
    public string Name => "ZipFiles";

    public IReadOnlyCollection<string> RequiredParameterKeys =>
        new[] { "SourceDirectory", "ZipFilePath" };

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        string pattern = args.GetValueOrDefault("SearchPattern", "*.*");

        string sourceDir = PathHelper.ResolveDynamicPath(args["SourceDirectory"]);
        string zipFilePath = PathHelper.ResolveDynamicPath(args["ZipFilePath"]);

        bool overwriteZip = bool.Parse(args.GetValueOrDefault("OverwriteZip", "true"));

        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Origen no encontrado: {sourceDir}");

        string? zipDir = Path.GetDirectoryName(zipFilePath);
        if (!string.IsNullOrEmpty(zipDir) && !Directory.Exists(zipDir))
            Directory.CreateDirectory(zipDir);

        if (File.Exists(zipFilePath) && overwriteZip)
            File.Delete(zipFilePath);

        string[] filesToZip = Directory.GetFiles(sourceDir, pattern);

        if (filesToZip.Length == 0)
        {
            WorkflowLogger.Log($"[Zip] No se encontraron archivos con el patrón '{pattern}' en {sourceDir}");
            return;
        }

        WorkflowLogger.Log($"[Zip] Comprimiendo {filesToZip.Length} archivos en: {zipFilePath}...");

        using (ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Update))
        {
            foreach (var file in filesToZip)
            {
                string fileName = Path.GetFileName(file);
                if (file == Path.GetFullPath(zipFilePath))
                    continue;

                archive.CreateEntryFromFile(file, fileName);
                WorkflowLogger.Log($"      Añadido: {fileName}");
            }
        }

        WorkflowLogger.Log("[Zip] Proceso de compresión completado.");
        await Task.CompletedTask;
    }
}
