using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Acquisition;

public sealed record AcquisitionCommandMetadata(
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int ExitCode,
    bool HasStandardError)
{
    public static AcquisitionCommandMetadata FromResult(SmartctlCommandResult result) =>
        new(
            result.ExecutablePath,
            result.Arguments,
            result.StartedAtUtc,
            result.CompletedAtUtc,
            result.ExitCode,
            !string.IsNullOrWhiteSpace(result.StandardError));
}
