using ChipGauge.Core.Models;

namespace ChipGauge.Core.Tests.Models;

public sealed class SensorTests
{
    [Fact]
    public void NewSensor_HasNoStatistics()
    {
        var sensor = new Sensor("id", "Temp", SensorKind.Temperature);

        Assert.Null(sensor.Value);
        Assert.Null(sensor.Min);
        Assert.Null(sensor.Max);
        Assert.Null(sensor.Average);
        Assert.Equal(0, sensor.SampleCount);
    }

    [Fact]
    public void Constructor_DefaultsUnitFromKind()
    {
        var sensor = new Sensor("id", "Temp", SensorKind.Temperature);
        Assert.Equal("°C", sensor.Unit);
    }

    [Fact]
    public void Constructor_HonorsExplicitUnit()
    {
        var sensor = new Sensor("id", "VRAM", SensorKind.Data, "GB");
        Assert.Equal("GB", sensor.Unit);
    }

    [Fact]
    public void Update_TracksLatestMinMaxAndAverage()
    {
        var sensor = new Sensor("id", "Load", SensorKind.Load);

        sensor.Update(40f);
        sensor.Update(20f);
        sensor.Update(60f);

        Assert.Equal(60f, sensor.Value);
        Assert.Equal(20f, sensor.Min);
        Assert.Equal(60f, sensor.Max);
        Assert.Equal(40f, sensor.Average);
        Assert.Equal(3, sensor.SampleCount);
    }

    [Fact]
    public void ResetStatistics_ReseedsFromCurrentValue()
    {
        var sensor = new Sensor("id", "Load", SensorKind.Load);
        sensor.Update(10f);
        sensor.Update(90f);

        sensor.ResetStatistics();

        Assert.Equal(90f, sensor.Value);
        Assert.Equal(90f, sensor.Min);
        Assert.Equal(90f, sensor.Max);
        Assert.Equal(90f, sensor.Average);
        Assert.Equal(1, sensor.SampleCount);
    }

    [Fact]
    public void ResetStatistics_WhenNeverSampled_ClearsToEmpty()
    {
        var sensor = new Sensor("id", "Load", SensorKind.Load);

        sensor.ResetStatistics();

        Assert.Null(sensor.Min);
        Assert.Null(sensor.Average);
        Assert.Equal(0, sensor.SampleCount);
    }

    [Fact]
    public void FormatValue_UsesInvariantCultureAndUnit()
    {
        var sensor = new Sensor("id", "Temp", SensorKind.Temperature);
        sensor.Update(42.5f);

        Assert.Equal("42.5 °C", sensor.FormatValue());
    }

    [Fact]
    public void Constructor_RejectsEmptyId()
    {
        Assert.Throws<ArgumentException>(() => new Sensor("", "n", SensorKind.Load));
    }
}
