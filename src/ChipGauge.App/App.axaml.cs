using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ChipGauge.App.Composition;
using ChipGauge.App.Services;
using ChipGauge.App.ViewModels;
using ChipGauge.App.Views;
using ChipGauge.Core.Monitoring;
using Microsoft.Extensions.DependencyInjection;

namespace ChipGauge.App;

public partial class App : Application
{
    /// <summary>The application service provider, available after framework initialization.</summary>
    public static IServiceProvider Services { get; private set; } = default!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        ServiceProvider provider = ServiceRegistration.Build();
        Services = provider;

        // Load persisted settings and apply them before the UI is built.
        var settings = provider.GetRequiredService<AppSettings>();
        var settingsService = provider.GetRequiredService<SettingsService>();
        settingsService.LoadInto(settings);

        var localizer = provider.GetRequiredService<Localizer>();
        Resources["Loc"] = localizer;
        localizer.SetLanguage(settings.Language);
        localizer.PublishResources(); // publish in the restored language
        DisplaySettings.TemperatureUnit = settings.TemperatureUnit;

        var themeService = provider.GetRequiredService<ThemeService>();
        themeService.Theme = settings.Theme;
        themeService.Apply();

        provider.GetRequiredService<MonitoringEngine>().Interval = TimeSpan.FromSeconds(settings.PollingSeconds);

        // From here on, changes are written back to disk.
        settingsService.PersistenceEnabled = true;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var engine = provider.GetRequiredService<MonitoringEngine>();
            var main = provider.GetRequiredService<MainWindowViewModel>();

            // Marshal each background sample onto the UI thread as a single tick.
            engine.Sampled += (_, _) => Dispatcher.UIThread.Post(main.OnTick);
            engine.Start();

            desktop.MainWindow = new MainWindow { DataContext = main };
            desktop.ShutdownRequested += async (_, _) =>
            {
                await engine.DisposeAsync();
                provider.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
