namespace ChipGauge.Core.Monitoring;

/// <summary>
/// Tunables for the <see cref="MonitoringEngine"/>. Defaults match the desktop
/// experience: sample once per second and retain a 60-second on-screen window.
/// </summary>
public sealed class MonitoringOptions
{
    /// <summary>The smallest polling interval the UI allows a user to select.</summary>
    public static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>The largest polling interval the UI allows a user to select.</summary>
    public static readonly TimeSpan MaxInterval = TimeSpan.FromSeconds(10);

    private TimeSpan _interval = TimeSpan.FromSeconds(1);
    private int _historyCapacity = 60;

    /// <summary>
    /// How often the engine polls the provider. Clamped to
    /// [<see cref="MinInterval"/>, <see cref="MaxInterval"/>].
    /// </summary>
    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            if (value < MinInterval)
            {
                value = MinInterval;
            }
            else if (value > MaxInterval)
            {
                value = MaxInterval;
            }

            _interval = value;
        }
    }

    /// <summary>Number of samples each sensor's history retains; must be positive.</summary>
    public int HistoryCapacity
    {
        get => _historyCapacity;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _historyCapacity = value;
        }
    }
}
