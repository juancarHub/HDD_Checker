namespace HddChecker.Core.Analysis;

public sealed record DeviceHealthAssessment(
    string Diagnosis,
    AssessmentConfidence Confidence,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Limitations,
    string Trajectory,
    string Recommendation);
