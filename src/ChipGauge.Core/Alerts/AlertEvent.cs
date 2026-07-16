namespace ChipGauge.Core.Alerts;

/// <summary>Describes an <see cref="AlertRule"/> transitioning into the firing state.</summary>
public sealed class AlertEvent
{
    /// <summary>Creates an alert event.</summary>
    /// <param name="rule">The rule that fired.</param>
    /// <param name="triggeredAt">When the rule fired.</param>
    /// <param name="value">The sensor value at the moment of firing.</param>
    public AlertEvent(AlertRule rule, DateTimeOffset triggeredAt, float value)
    {
        ArgumentNullException.ThrowIfNull(rule);
        Rule = rule;
        TriggeredAt = triggeredAt;
        Value = value;
    }

    /// <summary>The rule that fired.</summary>
    public AlertRule Rule { get; }

    /// <summary>When the rule fired.</summary>
    public DateTimeOffset TriggeredAt { get; }

    /// <summary>The sensor value at the moment of firing.</summary>
    public float Value { get; }
}
