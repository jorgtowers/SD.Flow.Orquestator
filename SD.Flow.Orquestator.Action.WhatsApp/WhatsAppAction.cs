using SD.Flow.Orquestator.Core;
using System.Diagnostics;
using System.Text;

public class WhatsAppAction : IWorkflowAction
{
    public string Name => "WhatsApp";

    public IReadOnlyCollection<string> RequiredParameterKeys => new[] { "Phone", "Message" };

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(3);

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        string phone = args["Phone"].Trim();
        string message = args["Message"];
        bool dryRun = args.GetValueOrDefault("DryRun", "false")
            .Equals("true", StringComparison.OrdinalIgnoreCase);

        string scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "send_ws.py");
        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"No se encontró el script de Python en: {scriptPath}");

        var (pythonExe, pythonArgPrefix) = ResolvePythonExecutable();
        int timeoutSeconds = int.TryParse(args.GetValueOrDefault("TimeoutSeconds", "180"), out int t) ? t : 180;
        var timeout = TimeSpan.FromSeconds(Math.Max(30, timeoutSeconds));

        var startInfo = new ProcessStartInfo
        {
            FileName = pythonExe,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (pythonArgPrefix != null)
            startInfo.ArgumentList.Add(pythonArgPrefix);

        startInfo.ArgumentList.Add(scriptPath);
        if (dryRun)
            startInfo.ArgumentList.Add("--dry-run");
        startInfo.ArgumentList.Add(phone);
        startInfo.ArgumentList.Add(message);

        WorkflowLogger.Log(dryRun
            ? $"[WhatsApp] Simulación de envío a {phone}..."
            : $"[WhatsApp] Enviando mensaje a {phone}...");

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
            throw new InvalidOperationException("No se pudo iniciar el proceso de Python para WhatsApp.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignorar fallo al matar proceso colgado
            }

            throw new TimeoutException(
                $"[WhatsApp] El script superó el tiempo límite de {timeout.TotalSeconds:0} segundos.");
        }

        string stdout = (await stdoutTask).Trim();
        string stderr = (await stderrTask).Trim();

        if (!string.IsNullOrEmpty(stdout))
            WorkflowLogger.Log($"[WhatsApp][stdout] {stdout}");

        if (!string.IsNullOrEmpty(stderr))
            WorkflowLogger.Log($"[WhatsApp][stderr] {stderr}");

        if (process.ExitCode != 0)
        {
            string detail = !string.IsNullOrEmpty(stderr) ? stderr : stdout;
            throw new InvalidOperationException(
                $"[WhatsApp] El script falló (código {process.ExitCode}). {detail}");
        }

        WorkflowLogger.Log("[WhatsApp] Proceso completado correctamente.");
    }

    private static (string FileName, string? ArgPrefix) ResolvePythonExecutable()
    {
        (string file, string? prefix, string versionArgs)[] candidates =
        [
            ("python", null, "--version"),
            ("python3", null, "--version"),
            ("py", "-3", "-3 --version"),
        ];

        foreach (var (file, prefix, versionArgs) in candidates)
        {
            try
            {
                var probe = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = versionArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(probe);
                if (process == null)
                    continue;

                process.WaitForExit(5000);
                if (process.ExitCode == 0)
                {
                    WorkflowLogger.Log($"[WhatsApp] Usando intérprete: {file}{(prefix != null ? " " + prefix : "")}");
                    return (file, prefix);
                }
            }
            catch
            {
                // probar siguiente candidato
            }
        }

        throw new FileNotFoundException(
            "No se encontró Python en PATH. Instale Python 3 y ejecute: npm run whatsapp:install");
    }
}
