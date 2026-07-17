using ChipGauge.App.Services;
using ChipGauge.App.ViewModels;
using ChipGauge.Core.Abstractions;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;
using ChipGauge.Core.Storage;

namespace ChipGauge.App.Tests;

public sealed class ElevationNoticeTests
{
    [Theory]
    [InlineData(true, false, true)]   // needs elevation, not elevated -> notice
    [InlineData(true, true, false)]   // needs elevation, elevated -> no notice
    [InlineData(false, false, false)] // no elevation needed -> no notice
    public async Task ShowElevationNotice_ReflectsProviderState(bool requiresElevation, bool isElevated, bool expected)
    {
        var provider = new FakeProvider(requiresElevation, isElevated);
        await using var engine = new MonitoringEngine(provider);
        var main = new MainWindowViewModel(
            new Localizer(), new ThemeService(), new AppSettings(), engine, provider, provider);

        Assert.Equal(expected, main.ShowElevationNotice);
    }

    private sealed class FakeProvider(bool requiresElevation, bool isElevated)
        : ISensorProvider, IHardwareInfoProvider
    {
        public string Name => "Fake";

        public bool IsAvailable => true;

        public bool RequiresElevation { get; } = requiresElevation;

        public bool IsSimulated => false;

        public bool IsElevated { get; } = isElevated;

        public IReadOnlyList<HardwareItem> GetHardware() => [];

        public IReadOnlyList<SensorReading> Sample() => [];

        public SystemSummary GetSystemSummary() => new() { OperatingSystem = "Test OS" };

        public IReadOnlyList<SmartDrive> GetSmartDrives() => [];

        public void Dispose()
        {
        }
    }
}
