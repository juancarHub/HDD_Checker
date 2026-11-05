using HddChecker.Core.Acquisition;

namespace HddChecker.Core.Longitudinal;

public sealed record LongitudinalTrend(
    DeviceCaptureHistory Device,
    IReadOnlyList<LongitudinalComparison> Intervals)
{
    public static LongitudinalTrend FromHistory(DeviceCaptureHistory device)
    {
        var intervals = device.Captures
            .Zip(device.Captures.Skip(1), (previous, current) =>
                LongitudinalComparer.Compare(previous.Package, current.Package))
            .ToArray();

        return new LongitudinalTrend(device, intervals);
    }
}
