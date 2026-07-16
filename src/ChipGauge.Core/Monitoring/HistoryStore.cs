using ChipGauge.Core.Models;

namespace ChipGauge.Core.Monitoring;

/// <summary>
/// Owns one <see cref="SensorHistory"/> per sensor id. Buffers are created lazily
/// on first record, so the store adapts to whatever sensors a provider exposes.
/// </summary>
public sealed class HistoryStore
{
    private readonly Dictionary<string, SensorHistory> _histories = new(StringComparer.Ordinal);
    private readonly int _capacity;

    /// <summary>Creates a store whose per-sensor buffers each retain <paramref name="capacity"/> samples.</summary>
    /// <param name="capacity">Per-sensor sample capacity; defaults to 60.</param>
    public HistoryStore(int capacity = 60)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
    }

    /// <summary>The set of sensor ids that currently have a history buffer.</summary>
    public IReadOnlyCollection<string> SensorIds => _histories.Keys;

    /// <summary>Returns the history for a sensor, creating an empty buffer if needed.</summary>
    public SensorHistory GetOrCreate(string sensorId)
    {
        ArgumentException.ThrowIfNullOrEmpty(sensorId);
        if (!_histories.TryGetValue(sensorId, out SensorHistory? history))
        {
            history = new SensorHistory(_capacity);
            _histories[sensorId] = history;
        }

        return history;
    }

    /// <summary>Attempts to get an existing history without creating one.</summary>
    public bool TryGet(string sensorId, out SensorHistory? history) =>
        _histories.TryGetValue(sensorId, out history);

    /// <summary>Records a value for a sensor at the given timestamp.</summary>
    public void Record(string sensorId, DateTimeOffset timestamp, float value) =>
        GetOrCreate(sensorId).Record(new TimedSample(timestamp, value));

    /// <summary>Clears every retained sample but keeps the buffers allocated.</summary>
    public void Clear()
    {
        foreach (SensorHistory history in _histories.Values)
        {
            history.Clear();
        }
    }
}
