using HddChecker.Core.Analysis;

namespace HddChecker.Agent;

internal sealed record AlertPresentation(
    string Title,
    string Message,
    string Recommendation,
    FindingSeverity? Severity,
    bool IsSimulation);
