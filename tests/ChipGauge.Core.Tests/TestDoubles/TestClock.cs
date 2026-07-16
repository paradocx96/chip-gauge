using ChipGauge.Core.Abstractions;

namespace ChipGauge.Core.Tests.TestDoubles;

/// <summary>A manually-advanced <see cref="IClock"/> for deterministic time-based tests.</summary>
internal sealed class TestClock : IClock
{
    public TestClock(DateTimeOffset? start = null) =>
        UtcNow = start ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public DateTimeOffset UtcNow { get; private set; }

    public void Advance(TimeSpan by) => UtcNow += by;

    public void AdvanceSeconds(double seconds) => Advance(TimeSpan.FromSeconds(seconds));
}
