using System.Diagnostics;

namespace HddChecker.Core.Smartctl;

public sealed class SmartctlRunner(string executablePath)
{
    public string ExecutablePath { get; } = executablePath;

    public async Task<SmartctlCommandResult> RunAsync(
        IEnumerable<string> arguments,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ExecutablePath))
        {
            throw new FileNotFoundException("smartctl executable was not found.", ExecutablePath);
        }

        var argumentList = arguments.ToArray();
        var startInfo = new ProcessStartInfo
        {
            FileName = ExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in argumentList)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var startedAt = DateTimeOffset.UtcNow;
        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException("smartctl could not be started.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return new SmartctlCommandResult(
            ExecutablePath,
            argumentList,
            startedAt,
            DateTimeOffset.UtcNow,
            process.ExitCode,
            await outputTask,
            await errorTask);
    }
}
