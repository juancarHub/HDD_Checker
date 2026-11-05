namespace HddChecker.Core.Smartctl;

public sealed record SmartctlResultAssessment(
    AcquisitionOutcome Outcome,
    SmartctlExitStatus ExitStatus,
    bool HasHealthWarning,
    bool HasHistoricalWarning)
{
    public bool HasUsableData => Outcome is not AcquisitionOutcome.Fatal;

    public static SmartctlResultAssessment FromExitCode(int exitCode)
    {
        var status = (SmartctlExitStatus)exitCode;
        var fatalMask = SmartctlExitStatus.CommandLineParseError |
                        SmartctlExitStatus.DeviceOpenFailed;
        var healthMask = SmartctlExitStatus.SmartStatusFailed |
                         SmartctlExitStatus.PrefailAttributeAtOrBelowThreshold;
        var historicalMask = SmartctlExitStatus.PrefailAttributeWasAtOrBelowThreshold |
                             SmartctlExitStatus.ErrorLogContainsRecords |
                             SmartctlExitStatus.SelfTestLogContainsRecords;

        var outcome = (status & fatalMask) != 0
            ? AcquisitionOutcome.Fatal
            : (status & SmartctlExitStatus.SmartCommandFailed) != 0
                ? AcquisitionOutcome.Partial
                : AcquisitionOutcome.Complete;

        return new SmartctlResultAssessment(
            outcome,
            status,
            (status & healthMask) != 0,
            (status & historicalMask) != 0);
    }
}
