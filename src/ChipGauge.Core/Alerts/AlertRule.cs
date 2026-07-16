namespace ChipGauge.Core.Alerts;

/// <summary>
/// A user-defined threshold rule such as "CPU package temperature above 90 °C for
/// 10 seconds". A rule fires only once its breach condition has held continuously
/// for <see cref="Duration"/>, which suppresses noise from momentary spikes.
/// </summary>
public sealed class AlertRule
{
    /// <summary>Creates an alert rule.</summary>
    /// <param name="id">Stable, unique rule identifier.</param>
    /// <param name="sensorId">The sensor the rule watches.</param>
    /// <param name="comparison">Whether the breach is above or below the threshold.</param>
    /// <param name="threshold">The threshold value in the sensor's unit.</param>
    /// <param name="duration">How long the breach must hold before firing; defaults to zero (instant).</param>
    /// <param name="message">Optional human-readable message for the notification.</param>
    public AlertRule(
        string id,
        string sensorId,
        AlertComparison comparison,
        float threshold,
        TimeSpan? duration = null,
        string? message = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(sensorId);
        if (duration is { } d && d < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration cannot be negative.");
        }

        Id = id;
        SensorId = sensorId;
        Comparison = comparison;
        Threshold = threshold;
        Duration = duration ?? TimeSpan.Zero;
        Message = message;
    }

    /// <summary>Stable, unique rule identifier.</summary>
    public string Id { get; }

    /// <summary>The sensor the rule watches.</summary>
    public string SensorId { get; }

    /// <summary>Whether the breach is triggered above or below the threshold.</summary>
    public AlertComparison Comparison { get; }

    /// <summary>The threshold value in the sensor's unit.</summary>
    public float Threshold { get; }

    /// <summary>How long the breach must hold continuously before the rule fires.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Optional human-readable message for the notification.</summary>
    public string? Message { get; }

    /// <summary>Returns true when <paramref name="value"/> breaches this rule's threshold.</summary>
    public bool IsBreached(float value) =>
        Comparison == AlertComparison.Above ? value > Threshold : value < Threshold;
}
