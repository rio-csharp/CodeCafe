using System.Diagnostics;

namespace CodeCafe.DbSync.Infrastructure;

internal sealed class ProcessRunner
{
    public void EnsureTool(string fileName)
    {
        var result = Run(fileName, "--version", sensitiveArguments: null, environmentVariables: null, allowExitCodeOne: false);
        if (result.ExitCode != 0)
        {
            throw new CommandException($"Required tool '{fileName}' was not found or could not run.");
        }
    }

    public CommandResult Run(
        string fileName,
        string arguments,
        string? sensitiveArguments = null,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        string? workingDirectory = null,
        bool allowExitCodeOne = false)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory
        };

        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
            {
                startInfo.EnvironmentVariables[key] = value;
            }
        }

        using var process = Process.Start(startInfo)
            ?? throw new CommandException($"Failed to start '{fileName}'.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        var allowed = process.ExitCode == 0 || (allowExitCodeOne && process.ExitCode == 1);
        if (!allowed)
        {
            var displayArguments = sensitiveArguments ?? arguments;
            throw new CommandException(
                $"Command failed with exit code {process.ExitCode}: {fileName} {displayArguments}{Environment.NewLine}{stderr}");
        }

        return new CommandResult(process.ExitCode, stdout, stderr);
    }
}

internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);
