using HddChecker.Core.Acquisition;
using HddChecker.Core.Smartctl;

namespace HddChecker.Core.Tests;

public sealed class DeviceMonitoringPolicyTests
{
    [Theory]
    [InlineData("usbprolific", null)]
    [InlineData("sntrealtek", null)]
    [InlineData("sntjmicron", null)]
    [InlineData("sat", null)]
    [InlineData("sat,12", null)]
    [InlineData("scsi", "/dev/sdd [USB Mass Storage]")]
    public void UsbBridgeTypesAreExternal(string type, string? infoName)
    {
        var device = new SmartctlDevice("/dev/test", type, "ATA", infoName);

        Assert.True(DeviceMonitoringPolicy.IsExternal(device));
    }

    [Theory]
    [InlineData("ata", "ATA device")]
    [InlineData("nvme", "NVMe device")]
    [InlineData("csmi", "CSMI device")]
    [InlineData("scsi", "SCSI device")]
    public void DirectControllerTypesAreMonitored(string type, string infoName)
    {
        var device = new SmartctlDevice("/dev/test", type, "ATA", infoName);

        Assert.False(DeviceMonitoringPolicy.IsExternal(device));
    }
}
