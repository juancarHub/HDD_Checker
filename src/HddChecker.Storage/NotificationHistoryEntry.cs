using HddChecker.Core.Analysis;

namespace HddChecker.Storage;

public sealed record NotificationHistoryEntry(
    string EventKey,
    string IdentityKey,
    string ModelName,
    string CaptureId,
    FindingSeverity Severity,
    string Title,
    string Message,
    string Recommendation,
    DateTimeOffset CreatedAtUtc);
