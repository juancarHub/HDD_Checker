using HddChecker.Core.Acquisition;
using HddChecker.Core.Analysis;

namespace HddChecker.Core.Longitudinal;

public sealed record LongitudinalTrend(
    DeviceCaptureHistory Device,
    IReadOnlyList<TrendInterval> Intervals)
{
    public static LongitudinalTrend FromHistory(DeviceCaptureHistory device)
    {
        var intervals = device.Captures
            .Zip(device.Captures.Skip(1), (previous, current) =>
            {
                var comparison = LongitudinalComparer.Compare(previous.Package, current.Package);
                return new TrendInterval(
                    comparison,
                    TrendRuleEvaluator.Evaluate(previous.Package, current.Package, comparison));
            })
            .ToArray();

        return new LongitudinalTrend(device, intervals);
    }
}
