using HddChecker.Core.Analysis;
using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Tests;

public sealed class DeviceHealthAssessmentEvaluatorTests
{
    [Fact]
    public void PartialHealthySsdExplainsMissingCountersAndSingleCapture()
    {
        var assessment = DeviceHealthAssessmentEvaluator.Evaluate(new DeviceHealthSignals(
            true,
            AcquisitionOutcome.Partial,
            "ATA",
            1,
            28,
            100,
            null,
            null,
            null,
            null,
            null,
            null));

        Assert.Equal("Sin indicios observados de problema", assessment.Diagnosis);
        Assert.Equal(AssessmentConfidence.Partial, assessment.Confidence);
        Assert.Contains(assessment.Limitations, item => item.Contains("reasignados"));
        Assert.Contains("solo existe una medición", assessment.Trajectory);
    }

    [Fact]
    public void CriticalFindingOverridesOtherwiseHealthySignals()
    {
        var assessment = DeviceHealthAssessmentEvaluator.Evaluate(new DeviceHealthSignals(
            true,
            AcquisitionOutcome.Complete,
            "NVMe",
            3,
            35,
            90,
            100,
            null,
            null,
            2,
            FindingSeverity.Critical,
            "Se han registrado errores de medios."));

        Assert.Equal("Problema crítico detectado", assessment.Diagnosis);
        Assert.Contains("sustitución inmediata", assessment.Recommendation);
    }
}
