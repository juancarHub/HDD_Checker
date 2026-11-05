using HddChecker.Core.Analysis;
using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Tests;

public sealed class SnapshotRuleEvaluatorTests
{
    [Fact]
    public void ExistingReallocatedSectorsAreAttentionNotLongitudinalIncrease()
    {
        const string json = """
        {
          "device": { "name": "/dev/test", "type": "ata", "protocol": "ATA" },
          "model_name": "SYNTHETIC ATA",
          "serial_number": "SYNTHETIC-001",
          "smart_status": { "passed": true },
          "ata_smart_attributes": {
            "table": [
              { "id": 5, "name": "Reallocated_Sector_Ct", "value": 98, "worst": 98, "thresh": 36, "raw": { "value": 41, "string": "41" } },
              { "id": 190, "name": "Airflow_Temperature_Cel", "value": 72, "worst": 38, "thresh": 45, "when_failed": "past", "raw": { "value": 28, "string": "28" } }
            ]
          }
        }
        """;
        var snapshot = SmartctlJsonParser.ParseSnapshot(json);

        var findings = SnapshotRuleEvaluator.Evaluate(snapshot);

        Assert.Contains(findings, finding =>
            finding.Code == "snapshot.ata.reallocated" &&
            finding.Severity == FindingSeverity.Attention);
        Assert.Contains(findings, finding =>
            finding.Code == "snapshot.ata.threshold.past" &&
            finding.Severity == FindingSeverity.Information);
    }
}
