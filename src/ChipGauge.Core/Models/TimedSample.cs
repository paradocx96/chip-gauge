namespace ChipGauge.Core.Models;

/// <summary>
/// A timestamped scalar retained in a sensor's rolling history. Kept as a
/// <see langword="readonly record struct"/> so history ring buffers store values
/// inline with no per-sample heap allocation.
/// </summary>
/// <param name="Timestamp">When the value was recorded (UTC).</param>
/// <param name="Value">The recorded value in the sensor's unit.</param>
public readonly record struct TimedSample(DateTimeOffset Timestamp, float Value);
