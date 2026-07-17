using ChipGauge.App.Services;
using ChipGauge.App.ViewModels;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;
using ChipGauge.Core.Reporting;
using ChipGauge.Hardware.Mock;

namespace ChipGauge.App.Tests;

public sealed class SettingsTests
{
    private static SettingsViewModel CreateSettings(MonitoringEngine engine, MockSensorProvider provider) =>
        new(new Localizer(), new ThemeService(), new AppSettings(),
            engine, new SettingsService(), new SnapshotService(), provider);

    [Fact]
    public void SettingsService_RoundTripsThroughDisk()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"chipgauge-{Guid.NewGuid():N}.json");
        try
        {
            var service = new SettingsService(path) { PersistenceEnabled = true };
            service.Save(new AppSettings
            {
                Theme = AppTheme.Dark,
                Language = "de",
                TemperatureUnit = TemperatureUnit.Fahrenheit,
                PollingSeconds = 2.5,
            });

            var loaded = new AppSettings();
            new SettingsService(path).LoadInto(loaded);

            Assert.Equal(AppTheme.Dark, loaded.Theme);
            Assert.Equal("de", loaded.Language);
            Assert.Equal(TemperatureUnit.Fahrenheit, loaded.TemperatureUnit);
            Assert.Equal(2.5, loaded.PollingSeconds);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Save_DoesNothing_WhenPersistenceDisabled()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"chipgauge-{Guid.NewGuid():N}.json");
        new SettingsService(path).Save(new AppSettings()); // PersistenceEnabled defaults to false
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task ChangingPollingSeconds_UpdatesEngineInterval()
    {
        using var provider = new MockSensorProvider();
        await using var engine = new MonitoringEngine(provider);
        SettingsViewModel settings = CreateSettings(engine, provider);

        settings.PollingSeconds = 3;

        Assert.Equal(TimeSpan.FromSeconds(3), engine.Interval);
    }

    [Fact]
    public async Task ChangingTemperatureUnit_UpdatesDisplaySettings()
    {
        try
        {
            using var provider = new MockSensorProvider();
            await using var engine = new MonitoringEngine(provider);
            SettingsViewModel settings = CreateSettings(engine, provider);

            settings.SelectedTemperatureUnit = TemperatureUnit.Fahrenheit;

            Assert.Equal(TemperatureUnit.Fahrenheit, DisplaySettings.TemperatureUnit);
        }
        finally
        {
            DisplaySettings.TemperatureUnit = TemperatureUnit.Celsius;
        }
    }

    [Fact]
    public void SensorViewModel_ConvertsTemperatureToFahrenheit_WhenSelected()
    {
        try
        {
            DisplaySettings.TemperatureUnit = TemperatureUnit.Fahrenheit;
            var sensor = new Sensor("t", "CPU Package", SensorKind.Temperature);
            var vm = new SensorViewModel(sensor, new SensorHistory());

            sensor.Update(100f); // 100 °C -> 212 °F
            vm.Refresh();

            Assert.Contains("212", vm.Value, StringComparison.Ordinal);
            Assert.Contains("°F", vm.Value, StringComparison.Ordinal);
        }
        finally
        {
            DisplaySettings.TemperatureUnit = TemperatureUnit.Celsius;
        }
    }

    [Fact]
    public async Task BuildSnapshotReport_ContainsSystemAndHardware()
    {
        using var provider = new MockSensorProvider();
        await using var engine = new MonitoringEngine(provider);
        engine.SampleOnce();
        SettingsViewModel settings = CreateSettings(engine, provider);

        string report = settings.BuildSnapshotReport();

        Assert.Contains("ChipGauge System Snapshot", report, StringComparison.Ordinal);
        Assert.Contains("Ryzen", report, StringComparison.Ordinal); // mock CPU name
    }
}
