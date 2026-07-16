using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;

namespace ChipGauge.Core.Tests.Monitoring;

public sealed class HistoryTests
{
    private static readonly DateTimeOffset Origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SensorHistory_RetainsRollingWindow()
    {
        var history = new SensorHistory(capacity: 3);
        for (var i = 0; i < 5; i++)
        {
            history.Record(new TimedSample(Origin.AddSeconds(i), i));
        }

        Assert.Equal(3, history.Count);
        Assert.Equal([2f, 3f, 4f], history.ToValueArray());
        Assert.Equal(4f, history.Latest!.Value.Value);
    }

    [Fact]
    public void SensorHistory_EmptyLatestIsNull()
    {
        var history = new SensorHistory();
        Assert.Null(history.Latest);
        Assert.Empty(history.ToValueArray());
    }

    [Fact]
    public void SensorHistory_SnapshotPreservesTimestamps()
    {
        var history = new SensorHistory(4);
        history.Record(new TimedSample(Origin, 1f));
        history.Record(new TimedSample(Origin.AddSeconds(1), 2f));

        IReadOnlyList<TimedSample> snapshot = history.Snapshot();

        Assert.Equal(2, snapshot.Count);
        Assert.Equal(Origin, snapshot[0].Timestamp);
        Assert.Equal(2f, snapshot[1].Value);
    }

    [Fact]
    public void HistoryStore_CreatesBufferLazilyPerSensor()
    {
        var store = new HistoryStore(capacity: 10);

        store.Record("cpu/temp", Origin, 50f);
        store.Record("gpu/temp", Origin, 60f);

        Assert.Equal(2, store.SensorIds.Count);
        Assert.True(store.TryGet("cpu/temp", out SensorHistory? cpu));
        Assert.Equal(50f, cpu!.Latest!.Value.Value);
    }

    [Fact]
    public void HistoryStore_UsesConfiguredCapacity()
    {
        var store = new HistoryStore(capacity: 2);
        for (var i = 0; i < 4; i++)
        {
            store.Record("s", Origin.AddSeconds(i), i);
        }

        store.TryGet("s", out SensorHistory? history);
        Assert.Equal(2, history!.Count);
        Assert.Equal([2f, 3f], history.ToValueArray());
    }

    [Fact]
    public void HistoryStore_TryGet_UnknownSensorReturnsFalse()
    {
        var store = new HistoryStore();
        Assert.False(store.TryGet("missing", out SensorHistory? history));
        Assert.Null(history);
    }

    [Fact]
    public void HistoryStore_Clear_EmptiesBuffersButKeepsSensors()
    {
        var store = new HistoryStore();
        store.Record("s", Origin, 1f);

        store.Clear();

        Assert.Contains("s", store.SensorIds);
        store.TryGet("s", out SensorHistory? history);
        Assert.Equal(0, history!.Count);
    }

    [Fact]
    public void HistoryStore_RejectsNonPositiveCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HistoryStore(0));
    }
}
