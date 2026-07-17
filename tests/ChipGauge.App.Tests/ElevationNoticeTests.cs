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
    [InlineData(true, false, true, true, false)]   // not elevated -> elevation notice
    [InlineData(true, true, true, false, false)]   // elevated, driver working -> no notice
    [InlineData(true, true, false, false, true)]   // elevated, driver blocked -> driver notice
    [InlineData(false, false, true, false, false)] // no elevation needed -> no notice
    public async Task Notices_ReflectProviderState(
        bool requiresElevation, bool isElevated, bool hasLowLevel, bool expectElevation, bool expectDriver)
    {
        var provider = new FakeProvider(requiresElevation, isElevated, hasLowLevel);
        await using var engine = new MonitoringEngine(provider);
        var main = new MainWindowViewModel(
            new Localizer(), new ThemeService(), new AppSettings(), new SettingsService(),
            engine, provider, provider, new Core.Reporting.SnapshotService(),
            new AlertRulesStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));

        Assert.Equal(expectElevation, main.ShowElevationNotice);
        Assert.Equal(expectDriver, main.ShowDriverNotice);
    }

    private sealed class FakeProvider(bool requiresElevation, bool isElevated, bool hasLowLevel)
        : ISensorProvider, IHardwareInfoProvider
    {
        public string Name => "Fake";

        public bool IsAvailable => true;

        public bool RequiresElevation { get; } = requiresElevation;

        public bool IsSimulated => false;

        public bool IsElevated { get; } = isElevated;

        public bool HasLowLevelSensors { get; } = hasLowLevel;

        public IReadOnlyList<HardwareItem> GetHardware() => [];

        public IReadOnlyList<SensorReading> Sample() => [];

        public SystemSummary GetSystemSummary() => new() { OperatingSystem = "Test OS" };

        public IReadOnlyList<SmartDrive> GetSmartDrives() => [];

        public void Dispose()
        {
        }
    }
}
