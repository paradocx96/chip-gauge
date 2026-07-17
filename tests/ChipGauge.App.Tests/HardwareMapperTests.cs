using System.Runtime.CompilerServices;
using ChipGauge.Core.Models;
using ChipGauge.Hardware.Windows;
using LibreHardwareMonitor.Hardware;

namespace ChipGauge.App.Tests;

/// <summary>
/// Tests for the LibreHardwareMonitor type mappers. LibreHardwareMonitor's
/// implementation assembly only ships for Windows runtimes, so these tests run on
/// Windows and skip elsewhere. Every LibreHardwareMonitor reference is confined to
/// <see cref="MethodImplOptions.NoInlining"/> helpers that are never JIT-compiled
/// off Windows, keeping the assembly loadable (and discoverable) on Linux CI.
/// </summary>
public sealed class HardwareMapperTests
{
    [Fact]
    public void HardwareTypes_MapToExpectedKinds()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("LibreHardwareMonitor is Windows-only.");
            return;
        }

        AssertHardwareMappings();
    }

    [Fact]
    public void EveryHardwareType_MapsToADefinedKind()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("LibreHardwareMonitor is Windows-only.");
            return;
        }

        AssertAllHardwareTypesMap();
    }

    [Fact]
    public void SensorTypes_MapToExpectedKindsAndUnits()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("LibreHardwareMonitor is Windows-only.");
            return;
        }

        AssertSensorMappings();
    }

    [Fact]
    public void EverySensorType_MapsToADefinedKind()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip("LibreHardwareMonitor is Windows-only.");
            return;
        }

        AssertAllSensorTypesMap();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertHardwareMappings()
    {
        Assert.Equal(HardwareKind.Cpu, HardwareTypeMapper.ToKind(HardwareType.Cpu));
        Assert.Equal(HardwareKind.Gpu, HardwareTypeMapper.ToKind(HardwareType.GpuNvidia));
        Assert.Equal(HardwareKind.Gpu, HardwareTypeMapper.ToKind(HardwareType.GpuAmd));
        Assert.Equal(HardwareKind.Gpu, HardwareTypeMapper.ToKind(HardwareType.GpuIntel));
        Assert.Equal(HardwareKind.Memory, HardwareTypeMapper.ToKind(HardwareType.Memory));
        Assert.Equal(HardwareKind.Storage, HardwareTypeMapper.ToKind(HardwareType.Storage));
        Assert.Equal(HardwareKind.Network, HardwareTypeMapper.ToKind(HardwareType.Network));
        Assert.Equal(HardwareKind.Motherboard, HardwareTypeMapper.ToKind(HardwareType.Motherboard));
        Assert.Equal(HardwareKind.Motherboard, HardwareTypeMapper.ToKind(HardwareType.SuperIO));
        Assert.Equal(HardwareKind.Motherboard, HardwareTypeMapper.ToKind(HardwareType.EmbeddedController));
        Assert.Equal(HardwareKind.Motherboard, HardwareTypeMapper.ToKind(HardwareType.Psu));
        Assert.Equal(HardwareKind.Cooler, HardwareTypeMapper.ToKind(HardwareType.Cooler));
        Assert.Equal(HardwareKind.Battery, HardwareTypeMapper.ToKind(HardwareType.Battery));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertAllHardwareTypesMap()
    {
        foreach (HardwareType type in Enum.GetValues<HardwareType>())
        {
            Assert.True(Enum.IsDefined(HardwareTypeMapper.ToKind(type)));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertSensorMappings()
    {
        AssertSensor(SensorType.Temperature, SensorKind.Temperature, "°C");
        AssertSensor(SensorType.Load, SensorKind.Load, "%");
        AssertSensor(SensorType.Clock, SensorKind.Clock, "MHz");
        AssertSensor(SensorType.Voltage, SensorKind.Voltage, "V");
        AssertSensor(SensorType.Power, SensorKind.Power, "W");
        AssertSensor(SensorType.Fan, SensorKind.Fan, "RPM");
        AssertSensor(SensorType.Data, SensorKind.Data, "GB");
        AssertSensor(SensorType.SmallData, SensorKind.Data, "MB");
        AssertSensor(SensorType.Throughput, SensorKind.Throughput, "B/s");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertAllSensorTypesMap()
    {
        foreach (SensorType type in Enum.GetValues<SensorType>())
        {
            Assert.True(Enum.IsDefined(SensorTypeMapper.Map(type).Kind));
        }
    }

    private static void AssertSensor(SensorType type, SensorKind expectedKind, string expectedUnit)
    {
        SensorMapping mapping = SensorTypeMapper.Map(type);
        Assert.Equal(expectedKind, mapping.Kind);
        Assert.Equal(expectedUnit, mapping.Unit);
    }
}
