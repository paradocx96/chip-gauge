using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;
using ChipGauge.Core.Tests.TestDoubles;
using ChipGauge.Hardware.Mock;

namespace ChipGauge.Core.Tests.Monitoring;

public sealed class MonitoringEngineTests
{
    [Fact]
    public async Task Constructor_IndexesEverySensorInTheTree()
    {
        using var provider = new MockSensorProvider(coreCount: 4);
        await using var engine = new MonitoringEngine(provider);

        int expected = provider.GetHardware().SelectMany(h => h.EnumerateSensors()).Count();
        Assert.Equal(expected, engine.SensorCount);
        Assert.True(engine.TryGetSensor("cpu/temp/package", out Sensor? sensor));
        Assert.Equal("CPU Package", sensor!.Name);
    }

    [Fact]
    public async Task SampleOnce_UpdatesSensorValuesAndStatistics()
    {
        var (item, sensor) = ScriptedSensorProvider.SingleSensor();
        var readings = new[] { 30f, 50f, 40f };
        var provider = new ScriptedSensorProvider([item], i => [new SensorReading("s1", readings[i])]);
        var clock = new TestClock();
        await using var engine = new MonitoringEngine(provider, clock: clock);

        foreach (float _ in readings)
        {
            engine.SampleOnce();
            clock.AdvanceSeconds(1);
        }

        Assert.Equal(40f, sensor.Value);
        Assert.Equal(30f, sensor.Min);
        Assert.Equal(50f, sensor.Max);
        Assert.Equal(40f, sensor.Average);
    }

    [Fact]
    public async Task SampleOnce_RecordsHistoryWithClockTimestamp()
    {
        var (item, _) = ScriptedSensorProvider.SingleSensor();
        var provider = new ScriptedSensorProvider([item], _ => [new SensorReading("s1", 42f)]);
        var clock = new TestClock();
        await using var engine = new MonitoringEngine(provider, clock: clock);

        engine.SampleOnce();

        Assert.True(engine.History.TryGet("s1", out SensorHistory? history));
        Assert.Equal(clock.UtcNow, history!.Latest!.Value.Timestamp);
        Assert.Equal(42f, history.Latest.Value.Value);
    }

    [Fact]
    public async Task SampleOnce_RaisesSampledEventWithReadings()
    {
        var (item, _) = ScriptedSensorProvider.SingleSensor();
        var provider = new ScriptedSensorProvider([item], _ => [new SensorReading("s1", 12f)]);
        await using var engine = new MonitoringEngine(provider);
        MonitoringSample? captured = null;
        engine.Sampled += (_, sample) => captured = sample;

        engine.SampleOnce();

        Assert.NotNull(captured);
        Assert.Single(captured!.Readings);
        Assert.Equal(12f, captured.Readings[0].Value);
    }

    [Fact]
    public async Task SampleOnce_IgnoresReadingsForUnknownSensors()
    {
        var (item, sensor) = ScriptedSensorProvider.SingleSensor();
        var provider = new ScriptedSensorProvider(
            [item],
            _ => [new SensorReading("s1", 5f), new SensorReading("ghost", 999f)]);
        await using var engine = new MonitoringEngine(provider);

        engine.SampleOnce();

        Assert.Equal(5f, sensor.Value);
        Assert.False(engine.History.TryGet("ghost", out _));
    }

    [Fact]
    public async Task ResetStatistics_ClearsRunningStatsOnAllSensors()
    {
        var (item, sensor) = ScriptedSensorProvider.SingleSensor();
        var values = new[] { 10f, 90f };
        var provider = new ScriptedSensorProvider([item], i => [new SensorReading("s1", values[i])]);
        await using var engine = new MonitoringEngine(provider);

        engine.SampleOnce();
        engine.SampleOnce();
        engine.ResetStatistics();

        Assert.Equal(90f, sensor.Min);
        Assert.Equal(90f, sensor.Max);
        Assert.Equal(1, sensor.SampleCount);
    }

    [Fact]
    public async Task StartAndStop_RunsBackgroundLoopThenStops()
    {
        var (item, sensor) = ScriptedSensorProvider.SingleSensor();
        var provider = new ScriptedSensorProvider([item], _ => [new SensorReading("s1", 33f)]);
        await using var engine = new MonitoringEngine(provider);

        engine.Start();
        // The loop emits an immediate first sample before the first timer tick.
        await WaitUntilAsync(() => sensor.Value is not null, TimeSpan.FromSeconds(2));
        await engine.StopAsync();

        Assert.False(engine.IsRunning);
        Assert.Equal(33f, sensor.Value);
    }

    [Fact]
    public async Task DisposeAsync_DisposesUnderlyingProvider()
    {
        var (item, _) = ScriptedSensorProvider.SingleSensor();
        var provider = new ScriptedSensorProvider([item], _ => []);
        var engine = new MonitoringEngine(provider);

        await engine.DisposeAsync();

        Assert.True(provider.Disposed);
    }

    [Fact]
    public async Task SampleOnce_AfterDispose_Throws()
    {
        var (item, _) = ScriptedSensorProvider.SingleSensor();
        var provider = new ScriptedSensorProvider([item], _ => []);
        var engine = new MonitoringEngine(provider);
        await engine.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => engine.SampleOnce());
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail("Condition was not met within the timeout.");
    }
}
