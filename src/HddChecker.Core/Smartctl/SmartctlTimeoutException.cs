namespace HddChecker.Core.Smartctl;

public sealed class SmartctlTimeoutException(
    string executablePath,
    IReadOnlyList<string> arguments,
    TimeSpan timeout)
    : TimeoutException($"smartctl superó el tiempo máximo de {FormatTimeout(timeout)}.")
{
    public string ExecutablePath { get; } = executablePath;
    public IReadOnlyList<string> Arguments { get; } = arguments;
    public TimeSpan Timeout { get; } = timeout;

    private static string FormatTimeout(TimeSpan value) =>
        value >= TimeSpan.FromSeconds(1)
            ? $"{value.TotalSeconds:0.##} segundos"
            : $"{value.TotalMilliseconds:0.##} milisegundos";
}
