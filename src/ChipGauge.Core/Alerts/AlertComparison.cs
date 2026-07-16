namespace ChipGauge.Core.Alerts;

/// <summary>The direction in which a sensor value breaches an <see cref="AlertRule"/> threshold.</summary>
public enum AlertComparison
{
    /// <summary>The rule breaches when the value rises strictly above the threshold.</summary>
    Above,

    /// <summary>The rule breaches when the value falls strictly below the threshold.</summary>
    Below,
}
