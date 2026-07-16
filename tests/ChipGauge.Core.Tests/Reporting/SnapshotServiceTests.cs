using ChipGauge.Core.Models;
using ChipGauge.Core.Reporting;
using ChipGauge.Core.Storage;

namespace ChipGauge.Core.Tests.Reporting;

public sealed class SnapshotServiceTests
{
    private static readonly DateTimeOffset Stamp = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    public void CreateTextReport_IncludesHeaderAndTimestamp()
    {
        var service = new SnapshotService();

        string report = service.CreateTextReport([], timestamp: Stamp);

        Assert.Contains("ChipGauge System Snapshot", report, StringComparison.Ordinal);
        Assert.Contains("2026-01-02 03:04:05Z", report, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateTextReport_RendersSystemSummary()
    {
        var service = new SnapshotService();
        var summary = new SystemSummary
        {
            OperatingSystem = "Windows 11 Pro",
            MachineName = "DESK-01",
            Architecture = "x64",
        };

        string report = service.CreateTextReport([], summary, timestamp: Stamp);

        Assert.Contains("[System]", report, StringComparison.Ordinal);
        Assert.Contains("Windows 11 Pro", report, StringComparison.Ordinal);
        Assert.Contains("DESK-01", report, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateTextReport_RendersHardwarePropertiesAndSensorStats()
    {
        var service = new SnapshotService();
        var sensor = new Sensor("cpu/temp", "CPU Package", SensorKind.Temperature);
        sensor.Update(50f);
        sensor.Update(70f);
        var cpu = new HardwareItem("cpu", "Test CPU", HardwareKind.Cpu)
            .SetProperty("Socket", "AM5")
            .AddSensor(sensor);

        string report = service.CreateTextReport([cpu], timestamp: Stamp);

        Assert.Contains("[Cpu] Test CPU", report, StringComparison.Ordinal);
        Assert.Contains("Socket: AM5", report, StringComparison.Ordinal);
        Assert.Contains("CPU Package = 70 °C", report, StringComparison.Ordinal);
        Assert.Contains("min 50", report, StringComparison.Ordinal);
        Assert.Contains("max 70", report, StringComparison.Ordinal);
        Assert.Contains("avg 60", report, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateTextReport_RendersSubHardwareIndented()
    {
        var service = new SnapshotService();
        var core = new HardwareItem("cpu/core0", "Core #0", HardwareKind.Cpu);
        var cpu = new HardwareItem("cpu", "Test CPU", HardwareKind.Cpu).AddSubHardware(core);

        string report = service.CreateTextReport([cpu], timestamp: Stamp);

        Assert.Contains("- Core #0", report, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateTextReport_RendersSmartHealthSection()
    {
        var service = new SnapshotService();
        var drive = new SmartDrive("d", "Test SSD")
        {
            Interface = "NVMe",
            TemperatureCelsius = 41,
            PowerOnHours = 1234,
        }
            .AddAttribute(new SmartAttribute(0x05, "Reallocated Sectors", 100, 100, 10, 0));

        string report = service.CreateTextReport([], drives: [drive], timestamp: Stamp);

        Assert.Contains("[Storage Health (S.M.A.R.T.)]", report, StringComparison.Ordinal);
        Assert.Contains("Test SSD — Health: Good", report, StringComparison.Ordinal);
        Assert.Contains("05 Reallocated Sectors", report, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateTextReport_IsDeterministicForSameInput()
    {
        var service = new SnapshotService();
        var cpu = new HardwareItem("cpu", "Test CPU", HardwareKind.Cpu).SetProperty("Socket", "AM5");

        string first = service.CreateTextReport([cpu], timestamp: Stamp);
        string second = service.CreateTextReport([cpu], timestamp: Stamp);

        Assert.Equal(first, second);
    }
}
