namespace HddChecker.Core.Smartctl;

public sealed record SmartctlCommandResult(
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int ExitCode,
    string StandardOutput,
    string StandardError);
