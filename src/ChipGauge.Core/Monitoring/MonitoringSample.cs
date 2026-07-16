using ChipGauge.Core.Models;

namespace ChipGauge.Core.Monitoring;

/// <summary>
/// The result of one sampling pass: every sensor reading captured at a single
/// instant. Raised by <see cref="MonitoringEngine.Sampled"/> so the UI can update
/// once per tick rather than once per sensor.
/// </summary>
public sealed class MonitoringSample
{
    /// <summary>Creates a sample.</summary>
    /// <param name="timestamp">The instant the readings were captured.</param>
    /// <param name="readings">The readings captured at that instant.</param>
    public MonitoringSample(DateTimeOffset timestamp, IReadOnlyList<SensorReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);
        Timestamp = timestamp;
        Readings = readings;
    }

    /// <summary>The instant the readings were captured (UTC).</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>The readings captured in this pass.</summary>
    public IReadOnlyList<SensorReading> Readings { get; }
}
