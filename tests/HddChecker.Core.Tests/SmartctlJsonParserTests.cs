using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Tests;

public sealed class SmartctlJsonParserTests
{
    [Fact]
    public void AtaSnapshotDistinguishesZeroFromUnavailableAndKeepsThresholdHistory()
    {
        const string json = """
        {
          "device": { "name": "/dev/test", "type": "ata", "protocol": "ATA" },
          "model_name": "SYNTHETIC ATA",
          "serial_number": "TEST-ATA-001",
          "smart_status": { "passed": true },
          "power_on_time": { "hours": 123 },
          "temperature": { "current": 29 },
          "ata_smart_attributes": {
            "table": [
              { "id": 5, "name": "Reallocated_Sector_Ct", "value": 100, "worst": 100, "thresh": 10, "raw": { "value": 0, "string": "0" } },
              { "id": 190, "name": "Airflow_Temperature_Cel", "value": 70, "worst": 40, "thresh": 45, "when_failed": "past", "raw": { "value": 29, "string": "29" } },
              { "id": 199, "name": "UDMA_CRC_Error_Count", "value": 200, "worst": 200, "thresh": 0, "raw": { "value": 0, "string": "0" } }
            ]
          }
        }
        """;

        var snapshot = SmartctlJsonParser.ParseSnapshot(json);

        Assert.Equal(0L, snapshot.ReallocatedSectors);
        Assert.Null(snapshot.PendingSectors);
        Assert.Equal(0L, snapshot.SataCrcErrors);
        var threshold = Assert.Single(snapshot.ThresholdEvents);
        Assert.Equal(190, threshold.Id);
        Assert.Equal("past", threshold.WhenFailed);
    }

    [Fact]
    public void NvmeSnapshotNormalizesEnduranceAndHealthCounters()
    {
        const string json = """
        {
          "device": { "name": "/dev/test", "type": "sntrealtek", "protocol": "NVMe" },
          "model_name": "SYNTHETIC NVME",
          "serial_number": "TEST-NVME-001",
          "user_capacity": { "bytes": 512000000000 },
          "smart_status": { "passed": true },
          "power_on_time": { "hours": 42 },
          "power_cycle_count": 7,
          "temperature": { "current": 35 },
          "endurance_used": { "current_percent": 4 },
          "nvme_smart_health_information_log": {
            "available_spare": 99,
            "data_units_read": 1000,
            "data_units_written": 2000,
            "unsafe_shutdowns": 3,
            "media_errors": 0,
            "num_err_log_entries": 12
          }
        }
        """;

        var snapshot = SmartctlJsonParser.ParseSnapshot(json);

        Assert.Equal(96, snapshot.RemainingLifePercent);
        Assert.Equal(99, snapshot.NvmeAvailableSparePercent);
        Assert.Equal(1000L, snapshot.NvmeDataUnitsRead);
        Assert.Equal(2000L, snapshot.NvmeDataUnitsWritten);
        Assert.Equal(3L, snapshot.NvmeUnsafeShutdowns);
        Assert.Equal(0L, snapshot.NvmeMediaErrors);
        Assert.Equal(12L, snapshot.NvmeErrorLogEntries);
    }
}
