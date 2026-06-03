using SD.Flow.Orquestator.Core;
using System.Diagnostics;
using System.Text;

public class ShellAction : IWorkflowAction
{
    public string Name => "ExecuteShell";

    public IReadOnlyCollection<string> RequiredParameterKeys => new[] { "Command" };

    public async Task ExecuteAsync(Dictionary<string, string> args)
    {
        string command = args["Command"].Trim();
        string shellType = NormalizeShellType(args.GetValueOrDefault("ShellType", "CMD"));
        bool wait = ParseBool(args.GetValueOrDefault("Wait", "true"), defaultValue: true);

        string workingDir = ResolveWorkingDirectory(args.GetValueOrDefault("WorkingDirectory", string.Empty));

        var startInfo = BuildStartInfo(shellType, command, workingDir, wait);

        WorkflowLogger.Log($"[Shell] Lanzando ({shellType}) [Wait={wait}] en '{workingDir}': {command}");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("No se pudo iniciar el proceso del shell.");

        if (!wait)
        {
            WorkflowLogger.Log("[Shell] Proceso lanzado en segundo plano.");
            return;
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        string output = await outputTask;
        string error = await errorTask;

        if (!string.IsNullOrWhiteSpace(output))
            WorkflowLogger.Log($"[Shell][stdout] {output.TrimEnd()}");

        if (!string.IsNullOrWhiteSpace(error))
            WorkflowLogger.Log($"[Shell][stderr] {error.TrimEnd()}");

        if (process.ExitCode != 0)
        {
            string detail = string.IsNullOrWhiteSpace(error)
                ? $"Código de salida: {process.ExitCode}"
                : error.Trim();
            throw new InvalidOperationException(
                $"El comando finalizó con código {process.ExitCode}. {detail}");
        }

        WorkflowLogger.Log("[Shell] Finalizado correctamente.");
    }

    private static ProcessStartInfo BuildStartInfo(string shellType, string command, string workingDir, bool wait)
    {
        var si = new ProcessStartInfo
        {
            WorkingDirectory = workingDir,
            CreateNoWindow = true
        };

        if (wait)
        {
            si.UseShellExecute = false;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            si.StandardOutputEncoding = Encoding.UTF8;
            si.StandardErrorEncoding = Encoding.UTF8;
        }
        else
        {
            si.UseShellExecute = true;
            si.WindowStyle = ProcessWindowStyle.Hidden;
        }

        if (shellType == "POWERSHELL")
            ConfigurePowerShell(si, command, workingDir, wait);
        else
            ConfigureCmd(si, command, workingDir, wait);

        return si;
    }

    private static void ConfigurePowerShell(ProcessStartInfo si, string command, string workingDir, bool wait)
    {
        si.FileName = ResolveExecutable("powershell.exe");

        bool isScript = TryResolveExistingScript(command, workingDir, out string fullCommand, ".ps1");

        si.ArgumentList.Add("-NoProfile");
        si.ArgumentList.Add("-NonInteractive");
        si.ArgumentList.Add("-ExecutionPolicy");
        si.ArgumentList.Add("Bypass");

        if (isScript)
        {
            si.ArgumentList.Add("-File");
            si.ArgumentList.Add(fullCommand);
            return;
        }

        si.ArgumentList.Add("-Command");
        si.ArgumentList.Add(command);
    }

    private static void ConfigureCmd(ProcessStartInfo si, string command, string workingDir, bool wait)
    {
        si.FileName = ResolveExecutable("cmd.exe");

        bool isBatch = TryResolveExistingScript(command, workingDir, out string fullCommand, ".bat", ".cmd");

        if (!wait)
        {
            si.ArgumentList.Add("/d");
            si.ArgumentList.Add("/c");
            si.ArgumentList.Add("start");
            si.ArgumentList.Add("\"\"");
            si.ArgumentList.Add(isBatch ? $"\"{fullCommand}\"" : command);
            return;
        }

        si.ArgumentList.Add("/d");
        si.ArgumentList.Add("/c");

        if (isBatch)
            si.ArgumentList.Add($"\"{fullCommand}\"");
        else
            si.ArgumentList.Add(command);
    }

    private static string ResolveWorkingDirectory(string? workingDirectory)
    {
        string dir = string.IsNullOrWhiteSpace(workingDirectory)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(PathHelper.ResolveDynamicPath(workingDirectory));

        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"WorkingDirectory no existe: {dir}");

        return dir;
    }

    private static string ResolveExecutable(string fileName)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
            return fileName;

        foreach (string folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(folder.Trim(), fileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return fileName;
    }

    private static string NormalizeShellType(string shellType)
    {
        string normalized = shellType.Trim().ToUpperInvariant();
        return normalized switch
        {
            "POWERSHELL" or "PS" or "PWSH" => "POWERSHELL",
            "CMD" or "COMMAND" or "BAT" => "CMD",
            _ => throw new ArgumentException(
                $"ShellType '{shellType}' no soportado. Use CMD o POWERSHELL.")
        };
    }

    private static bool ParseBool(string value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;
        return bool.TryParse(value, out bool parsed) ? parsed : defaultValue;
    }

    private static bool TryResolveExistingScript(
        string command,
        string workingDir,
        out string fullPath,
        params string[] extensions)
    {
        fullPath = command;
        if (string.IsNullOrWhiteSpace(command))
            return false;

        try
        {
            fullPath = Path.IsPathRooted(command)
                ? Path.GetFullPath(command)
                : Path.GetFullPath(Path.Combine(workingDir, command));
        }
        catch
        {
            return false;
        }

        if (!File.Exists(fullPath))
            return false;

        foreach (string ext in extensions)
        {
            if (fullPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
