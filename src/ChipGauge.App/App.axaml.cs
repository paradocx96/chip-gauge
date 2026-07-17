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

        // Publish localized strings into the resource dictionary so the Localize
        // markup extension (a DynamicResource) can resolve and live-update them.
        var localizer = provider.GetRequiredService<Localizer>();
        Resources["Loc"] = localizer;
        localizer.PublishResources();

        // Apply the persisted theme before the first window shows.
        provider.GetRequiredService<ThemeService>().Apply();

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
