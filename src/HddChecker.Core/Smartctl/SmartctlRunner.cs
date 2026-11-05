using System.Diagnostics;

namespace HddChecker.Core.Smartctl;

public sealed class SmartctlRunner
{
    public SmartctlRunner(string executablePath, TimeSpan? timeout = null)
    {
        ExecutablePath = executablePath;
        Timeout = timeout ?? TimeSpan.FromSeconds(30);
        if (Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "El tiempo máximo debe ser positivo.");
        }
    }

    public string ExecutablePath { get; }
    public TimeSpan Timeout { get; }

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

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(Timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryTerminate(process);
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(outputTask, errorTask);
            throw new SmartctlTimeoutException(ExecutablePath, argumentList, Timeout);
        }

        return new SmartctlCommandResult(
            ExecutablePath,
            argumentList,
            startedAt,
            DateTimeOffset.UtcNow,
            process.ExitCode,
            await outputTask,
            await errorTask);
    }

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited between the state check and termination.
        }
    }
}
