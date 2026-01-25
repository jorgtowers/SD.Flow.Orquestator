using SD.Flow.Orquestator.Core;
using System.Diagnostics;

public class ShellAction : IWorkflowAction
{
    public string Name => "ExecuteShell";

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        string command = args["Command"];
        string type = args.GetValueOrDefault("ShellType", "CMD").ToUpper();
        string workingDir = args.GetValueOrDefault("WorkingDirectory", Directory.GetCurrentDirectory());

        // Nuevo parámetro: por defecto es true para mantener seguridad, pero puedes pasar "false"
        bool wait = bool.Parse(args.GetValueOrDefault("Wait", "true"));

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.WorkingDirectory = workingDir;

        if (wait)
        {
            // MODO SÍNCRONO: Esperamos y capturamos logs
            ConfigurarModoEspera(startInfo, type, command);
        }
        else
        {
            // MODO ASÍNCRONO: Disparar y olvidar
            ConfigurarModoFuegoYOlvido(startInfo, type, command);
        }

        Console.WriteLine($"[Shell] Lanzando ({type}) [Wait={wait}]: {command}");

        using (Process? process = Process.Start(startInfo))
        {
            if (process == null) throw new Exception("No se pudo iniciar el proceso.");

            if (wait)
            {
                // Capturamos salida solo si estamos esperando
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(output)) Console.WriteLine($"[Output]: {output}");

                if (process.ExitCode != 0)
                    throw new Exception($"Falló con código {process.ExitCode}. Error: {error}");
            }
            // Si wait es false, el 'using' cerrará el handle del proceso padre, 
            // pero el proceso hijo seguirá vivo en Windows.
        }

        Console.WriteLine(wait ? "[Shell] Finalizado." : "[Shell] Lanzado e independiente.");
    }

    private void ConfigurarModoEspera(ProcessStartInfo si, string type, string cmd)
    {
        si.FileName = type == "POWERSHELL" ? "powershell.exe" : "cmd.exe";
        si.Arguments = type == "POWERSHELL"
            ? $"-NoProfile -ExecutionPolicy Bypass -File \"{cmd}\""
            : $"/c {cmd}";

        si.RedirectStandardOutput = true;
        si.RedirectStandardError = true;
        si.UseShellExecute = false;
        si.CreateNoWindow = true;
    }

    private void ConfigurarModoFuegoYOlvido(ProcessStartInfo si, string type, string cmd)
    {
        if (type == "POWERSHELL")
        {
            si.FileName = "powershell.exe";
            si.Arguments = $"-NoProfile -Command \"Start-Process powershell.exe -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"\"{cmd}\"\"'\"";
        }
        else
        {
            si.FileName = "cmd.exe";
            si.Arguments = $"/c start \"\" {cmd}";
        }

        si.RedirectStandardOutput = false;
        si.RedirectStandardError = false;
        si.UseShellExecute = true; // Crucial para que 'start' funcione
        si.CreateNoWindow = true;
    }
}