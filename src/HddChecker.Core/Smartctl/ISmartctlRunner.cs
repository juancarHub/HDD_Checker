namespace HddChecker.Core.Smartctl;

public interface ISmartctlRunner
{
    Task<SmartctlCommandResult> RunAsync(
        IEnumerable<string> arguments,
        CancellationToken cancellationToken = default);
}
