using ChipGauge.Core.Models;

namespace ChipGauge.Core.Monitoring;

/// <summary>
/// The rolling time-series for a single sensor, backed by a <see cref="RingBuffer{T}"/>.
/// The default capacity of 60 mirrors the 60-second window a 1 Hz sampler fills —
/// the same look as the Task Manager performance graphs.
/// </summary>
public sealed class SensorHistory
{
    private readonly RingBuffer<TimedSample> _samples;

    /// <summary>Creates a history buffer.</summary>
    /// <param name="capacity">Number of samples to retain; defaults to 60.</param>
    public SensorHistory(int capacity = 60) => _samples = new RingBuffer<TimedSample>(capacity);

    /// <summary>The number of samples retained.</summary>
    public int Count => _samples.Count;

    /// <summary>The maximum number of samples retained.</summary>
    public int Capacity => _samples.Capacity;

    /// <summary>The most recent sample, or <see langword="null"/> when empty.</summary>
    public TimedSample? Latest => _samples.Count > 0 ? _samples.Newest : null;

    /// <summary>Appends a sample, evicting the oldest once at capacity.</summary>
    public void Record(TimedSample sample) => _samples.Add(sample);

    /// <summary>Returns the retained samples, oldest first.</summary>
    public IReadOnlyList<TimedSample> Snapshot() => _samples.ToArray();

    /// <summary>
    /// Copies just the values, oldest first, into a fresh array — the shape a chart
    /// control consumes to plot the line.
    /// </summary>
    public float[] ToValueArray()
    {
        var values = new float[_samples.Count];
        for (var i = 0; i < _samples.Count; i++)
        {
            values[i] = _samples[i].Value;
        }

        return values;
    }

    /// <summary>Removes all retained samples.</summary>
    public void Clear() => _samples.Clear();
}
