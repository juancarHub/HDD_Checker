using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Tests;

public sealed class SmartctlResultAssessmentTests
{
    [Fact]
    public void RunnerRejectsNonPositiveTimeout()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SmartctlRunner("smartctl", TimeSpan.Zero));
    }

    [Fact]
    public void CommandFailureWithUsableJsonIsPartial()
    {
        var assessment = SmartctlResultAssessment.FromExitCode(4);

        Assert.Equal(AcquisitionOutcome.Partial, assessment.Outcome);
        Assert.True(assessment.HasUsableData);
        Assert.False(assessment.HasHealthWarning);
    }

    [Fact]
    public void HistoricalThresholdBitIsNotCurrentHealthFailure()
    {
        var assessment = SmartctlResultAssessment.FromExitCode(32);

        Assert.Equal(AcquisitionOutcome.Complete, assessment.Outcome);
        Assert.False(assessment.HasHealthWarning);
        Assert.True(assessment.HasHistoricalWarning);
    }

    [Fact]
    public void DeviceOpenFailureIsFatal()
    {
        var assessment = SmartctlResultAssessment.FromExitCode(2);

        Assert.Equal(AcquisitionOutcome.Fatal, assessment.Outcome);
        Assert.False(assessment.HasUsableData);
    }
}
