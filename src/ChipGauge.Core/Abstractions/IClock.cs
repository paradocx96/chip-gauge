namespace ChipGauge.Core.Abstractions;

/// <summary>
/// Abstraction over the system clock so that time-dependent logic (sampling,
/// alert durations, history timestamps) can be driven deterministically in tests.
/// </summary>
public interface IClock
{
    /// <summary>Gets the current time as a UTC <see cref="DateTimeOffset"/>.</summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>Default <see cref="IClock"/> backed by <see cref="DateTimeOffset.UtcNow"/>.</summary>
public sealed class SystemClock : IClock
{
    /// <summary>A shared, thread-safe instance.</summary>
    public static readonly SystemClock Instance = new();

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
