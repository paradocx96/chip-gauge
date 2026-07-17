using ChipGauge.App.Composition;
using ChipGauge.App.Services;
using ChipGauge.App.ViewModels;
using ChipGauge.Core.Monitoring;
using Microsoft.Extensions.DependencyInjection;

namespace ChipGauge.App.Tests;

public sealed class ViewModelTests
{
    private static ServiceProvider BuildWarmedProvider(int ticks = 5)
    {
        ServiceProvider provider = ServiceRegistration.Build();
        var engine = provider.GetRequiredService<MonitoringEngine>();
        for (var i = 0; i < ticks; i++)
        {
            engine.SampleOnce();
        }

        return provider;
    }

    [Fact]
    public async Task Shell_ExposesAllNineNavigationPages()
    {
        await using ServiceProvider provider = BuildWarmedProvider();
        var main = provider.GetRequiredService<MainWindowViewModel>();

        Assert.Equal(9, main.Pages.Count);
        Assert.IsType<DashboardViewModel>(main.SelectedPage);
        Assert.Contains(main.Pages, p => p is SensorsViewModel);
        Assert.Contains(main.Pages, p => p is SettingsViewModel);
    }

    [Fact]
    public async Task Dashboard_BuildsTilesForKeyComponents()
    {
        await using ServiceProvider provider = BuildWarmedProvider();
        var dashboard = (DashboardViewModel)provider.GetRequiredService<MainWindowViewModel>().Pages[0];

        // CPU, GPU, memory, two disks and network.
        Assert.Equal(6, dashboard.Tiles.Count);
        Assert.All(dashboard.Tiles, tile => Assert.NotEqual("—", tile.Primary.Value));
    }

    [Fact]
    public async Task SelectingCategory_ShowsDevicesWithLiveValues()
    {
        await using ServiceProvider provider = BuildWarmedProvider();
        var main = provider.GetRequiredService<MainWindowViewModel>();

        CategoryViewModel cpu = main.Pages.OfType<CategoryViewModel>()
            .First(c => c.Kind == Core.Models.HardwareKind.Cpu);
        main.SelectedPage = cpu;

        Assert.True(cpu.HasData);
        SensorViewModel sensor = cpu.Items[0].Sensors[0];
        Assert.NotEqual("—", sensor.Value);
    }

    [Fact]
    public async Task Sensors_FilterNarrowsRows()
    {
        await using ServiceProvider provider = BuildWarmedProvider();
        var sensors = provider.GetRequiredService<MainWindowViewModel>().Pages.OfType<SensorsViewModel>().Single();

        int total = sensors.Rows.Count();
        sensors.Filter = "GPU";
        List<SensorRow> filtered = [.. sensors.Rows];

        Assert.True(filtered.Count < total);
        Assert.NotEmpty(filtered);
        Assert.All(filtered, r => Assert.True(
            r.Device.Contains("GPU", StringComparison.OrdinalIgnoreCase) ||
            r.Sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task CycleThemeCommand_AdvancesTheme()
    {
        await using ServiceProvider provider = BuildWarmedProvider();
        var main = provider.GetRequiredService<MainWindowViewModel>();

        Assert.Equal(AppTheme.System, main.Theme.Theme);
        main.CycleThemeCommand.Execute(null);
        Assert.Equal(AppTheme.Light, main.Theme.Theme);
        main.CycleThemeCommand.Execute(null);
        Assert.Equal(AppTheme.Dark, main.Theme.Theme);
        main.CycleThemeCommand.Execute(null);
        Assert.Equal(AppTheme.System, main.Theme.Theme);
    }

    [Fact]
    public async Task SensorViewModel_Refresh_ReflectsUpdatedEngineValue()
    {
        await using ServiceProvider provider = ServiceRegistration.Build();
        var engine = provider.GetRequiredService<MonitoringEngine>();
        engine.SampleOnce();

        var main = provider.GetRequiredService<MainWindowViewModel>();
        SensorViewModel sensor = main.Pages.OfType<SensorsViewModel>().Single().Rows.First().Sensor;

        string before = sensor.Value;
        Assert.NotEqual("—", before);
        Assert.NotEmpty(sensor.Values);
    }

    [Fact]
    public async Task SettingsViewModel_ChangingLanguage_UpdatesLocalizer()
    {
        await using ServiceProvider provider = BuildWarmedProvider();
        var settings = provider.GetRequiredService<MainWindowViewModel>().Pages.OfType<SettingsViewModel>().Single();
        var localizer = provider.GetRequiredService<Localizer>();

        LanguageOption german = Localizer.Languages.First(l => l.Code == "de");
        settings.SelectedLanguage = german;

        Assert.Equal("de", localizer.CurrentLanguage);
    }
}
