using HddChecker.Core.Analysis;

namespace HddChecker.Core.Longitudinal;

public sealed record TrendInterval(
    LongitudinalComparison Comparison,
    IReadOnlyList<TrendFinding> Findings);
