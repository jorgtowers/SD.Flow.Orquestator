namespace SD.Flow.Orquestator.Actions
{
    using SD.Flow.Orquestator.Core;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public class MoveFileAction : IWorkflowAction
    {
        public string Name => "MoveFile";

        public async Task ExecuteAsync(Dictionary<string, string> args)
        {
            if (!args.ContainsKey("Source") || !args.ContainsKey("Destination"))
            {
                throw new ArgumentException("MoveFile requiere los parámetros 'Source' y 'Destination'.");
            }

            string sourcePath = PathHelper.ResolveDynamicPath(args["Source"]);
            string destinationPath = PathHelper.ResolveDynamicPath(args["Destination"]);

            // Validar si el origen existe
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"El archivo origen no existe: {sourcePath}");
            }

            // Obtener el directorio de destino y crearlo si no existe
            string? destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
                Console.WriteLine($"[Move] Directorio creado: {destinationDirectory}");
            }

            // Mover el archivo (true para sobrescribir si ya existe en .NET 7+)
            // Si usas versiones antiguas, hay que borrar el destino primero.
            File.Move(sourcePath, destinationPath, overwrite: true);

            Console.WriteLine($"[Move] Archivo movido con éxito:");
            Console.WriteLine($"      Desde: {sourcePath}");
            Console.WriteLine($"      Hacia: {destinationPath}");

            await Task.CompletedTask;
        }
    }
}
