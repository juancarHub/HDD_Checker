namespace HddChecker.Core.Longitudinal;

public sealed record MetricChange(
    string Name,
    long? Previous,
    long? Current,
    long? Delta,
    string Unit)
{
    public bool IsComparable => Previous is not null && Current is not null;
    public bool HasChanged => IsComparable && Delta != 0;

    public static MetricChange Create(
        string name,
        long? previous,
        long? current,
        string unit = "") =>
        new(
            name,
            previous,
            current,
            previous is not null && current is not null ? current - previous : null,
            unit);
}
