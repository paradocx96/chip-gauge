using ChipGauge.Core.Monitoring;

namespace ChipGauge.Core.Tests.Monitoring;

public sealed class MonitoringOptionsTests
{
    [Fact]
    public void Interval_ClampsBelowMinimum()
    {
        var options = new MonitoringOptions { Interval = TimeSpan.FromMilliseconds(10) };
        Assert.Equal(MonitoringOptions.MinInterval, options.Interval);
    }

    [Fact]
    public void Interval_ClampsAboveMaximum()
    {
        var options = new MonitoringOptions { Interval = TimeSpan.FromMinutes(5) };
        Assert.Equal(MonitoringOptions.MaxInterval, options.Interval);
    }

    [Fact]
    public void Interval_KeepsValueWithinRange()
    {
        var options = new MonitoringOptions { Interval = TimeSpan.FromSeconds(2) };
        Assert.Equal(TimeSpan.FromSeconds(2), options.Interval);
    }

    [Fact]
    public void Defaults_MatchDesktopExperience()
    {
        var options = new MonitoringOptions();
        Assert.Equal(TimeSpan.FromSeconds(1), options.Interval);
        Assert.Equal(60, options.HistoryCapacity);
    }

    [Fact]
    public void HistoryCapacity_RejectsNonPositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonitoringOptions { HistoryCapacity = 0 });
    }
}
