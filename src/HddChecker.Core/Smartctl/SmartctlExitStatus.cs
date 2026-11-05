namespace HddChecker.Core.Smartctl;

[Flags]
public enum SmartctlExitStatus
{
    None = 0,
    CommandLineParseError = 1 << 0,
    DeviceOpenFailed = 1 << 1,
    SmartCommandFailed = 1 << 2,
    SmartStatusFailed = 1 << 3,
    PrefailAttributeAtOrBelowThreshold = 1 << 4,
    PrefailAttributeWasAtOrBelowThreshold = 1 << 5,
    ErrorLogContainsRecords = 1 << 6,
    SelfTestLogContainsRecords = 1 << 7,
}
