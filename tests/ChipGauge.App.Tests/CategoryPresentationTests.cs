using ChipGauge.App.Services;
using ChipGauge.App.ViewModels;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;

namespace ChipGauge.App.Tests;

public sealed class CategoryPresentationTests
{
    [Fact]
    public void HardwareItem_HidesSensorsWithoutReadings_AndGroupsByKind()
    {
        var load = new Sensor("s1", "Total Load", SensorKind.Load);
        var temp = new Sensor("s2", "Package", SensorKind.Temperature);
        var dead = new Sensor("s3", "Unavailable", SensorKind.Power); // never produces a reading

        var device = new HardwareItem("hw", "Device", HardwareKind.Cpu)
            .AddSensor(load)
            .AddSensor(temp)
            .AddSensor(dead);

        var vm = new HardwareItemViewModel(device, new HistoryStore(), new Localizer());

        // Before any reading, nothing is visible.
        Assert.False(vm.HasVisibleSensors);
        Assert.Empty(vm.SensorGroups);

        load.Update(45f);
        temp.Update(60f);
        vm.Refresh();

        Assert.True(vm.HasVisibleSensors);
        Assert.Equal(2, vm.VisibleSensors.Count);
        Assert.DoesNotContain(vm.VisibleSensors, s => s.Name == "Unavailable");

        // One group per represented kind (Load, Temperature), Load ordered first.
        Assert.Equal(2, vm.SensorGroups.Count);
        Assert.Single(vm.SensorGroups[0].Sensors);
        Assert.Equal("Total Load", vm.SensorGroups[0].Sensors[0].Name);
    }

    [Fact]
    public void ThroughputSensor_FormatsBytesPerSecondHumanReadably()
    {
        var sensor = new Sensor("net", "Download", SensorKind.Throughput);
        var vm = new SensorViewModel(sensor, new SensorHistory());

        sensor.Update(1_500_000f); // 1.5 MB/s
        vm.Refresh();

        Assert.Contains("MB/s", vm.Value, StringComparison.Ordinal);
    }
}
