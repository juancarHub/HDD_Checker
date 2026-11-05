namespace HddChecker.Core.Analysis;

public sealed record TrendFinding(
    string Code,
    FindingSeverity Severity,
    string Message);
