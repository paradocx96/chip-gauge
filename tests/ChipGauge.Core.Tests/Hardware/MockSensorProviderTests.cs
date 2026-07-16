using ChipGauge.Core.Models;
using ChipGauge.Core.Storage;
using ChipGauge.Hardware.Mock;

namespace ChipGauge.Core.Tests.Hardware;

public sealed class MockSensorProviderTests
{
    [Fact]
    public void GetHardware_ExposesEveryCategory()
    {
        using var provider = new MockSensorProvider();

        HardwareKind[] kinds = [.. provider.GetHardware().Select(h => h.Kind).Distinct()];

        Assert.Contains(HardwareKind.Cpu, kinds);
        Assert.Contains(HardwareKind.Gpu, kinds);
        Assert.Contains(HardwareKind.Memory, kinds);
        Assert.Contains(HardwareKind.Storage, kinds);
        Assert.Contains(HardwareKind.Network, kinds);
        Assert.Contains(HardwareKind.Motherboard, kinds);
    }

    [Fact]
    public void Cpu_HasRequestedNumberOfCores()
    {
        using var provider = new MockSensorProvider(coreCount: 6);

        HardwareItem cpu = provider.GetHardware().Single(h => h.Kind == HardwareKind.Cpu);

        Assert.Equal(6, cpu.SubHardware.Count);
    }

    [Fact]
    public void Sample_ReturnsOneReadingPerSensor()
    {
        using var provider = new MockSensorProvider();
        int sensorCount = provider.GetHardware().SelectMany(h => h.EnumerateSensors()).Count();

        IReadOnlyList<SensorReading> readings = provider.Sample();

        Assert.Equal(sensorCount, readings.Count);
    }

    [Fact]
    public void Sample_IsDeterministicForSameSeed()
    {
        using var a = new MockSensorProvider(seed: 42);
        using var b = new MockSensorProvider(seed: 42);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(a.Sample(), b.Sample());
        }
    }

    [Fact]
    public void Sample_DiffersBetweenSeeds()
    {
        using var a = new MockSensorProvider(seed: 1);
        using var b = new MockSensorProvider(seed: 2);

        // Same structure, but noise should make at least one reading differ.
        IReadOnlyList<SensorReading> first = a.Sample();
        IReadOnlyList<SensorReading> second = b.Sample();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Sample_RespectsConfiguredBounds()
    {
        using var provider = new MockSensorProvider(seed: 7);
        Dictionary<string, Sensor> sensors = provider.GetHardware()
            .SelectMany(h => h.EnumerateSensors())
            .ToDictionary(s => s.Id, StringComparer.Ordinal);

        for (var i = 0; i < 200; i++)
        {
            foreach (SensorReading reading in provider.Sample())
            {
                Sensor sensor = sensors[reading.SensorId];
                if (sensor.Kind.IsPercentage())
                {
                    Assert.InRange(reading.Value, 0f, 100f);
                }

                Assert.False(float.IsNaN(reading.Value));
            }
        }
    }

    [Fact]
    public void Sample_IncrementsTick()
    {
        using var provider = new MockSensorProvider();

        Assert.Equal(0, provider.Tick);
        provider.Sample();
        provider.Sample();
        Assert.Equal(2, provider.Tick);
    }

    [Fact]
    public void GetSmartDrives_ReturnsHealthyDrives()
    {
        using var provider = new MockSensorProvider();

        IReadOnlyList<SmartDrive> drives = provider.GetSmartDrives();

        Assert.Equal(2, drives.Count);
        Assert.All(drives, d => Assert.Equal(DriveHealth.Good, d.Health));
    }

    [Fact]
    public void GetSystemSummary_IsPopulated()
    {
        using var provider = new MockSensorProvider();

        SystemSummary summary = provider.GetSystemSummary();

        Assert.False(string.IsNullOrWhiteSpace(summary.OperatingSystem));
        Assert.NotNull(summary.MachineName);
    }

    [Fact]
    public void Provider_ReportsAvailableAndNoElevation()
    {
        using var provider = new MockSensorProvider();

        Assert.True(provider.IsAvailable);
        Assert.False(provider.RequiresElevation);
    }
}
