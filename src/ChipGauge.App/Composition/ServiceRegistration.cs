using ChipGauge.App.Services;
using ChipGauge.App.ViewModels;
using ChipGauge.Core.Abstractions;
using ChipGauge.Core.Monitoring;
using ChipGauge.Core.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace ChipGauge.App.Composition;

/// <summary>
/// The application's composition root. The hardware provider is chosen at runtime
/// by <see cref="HardwareProviderFactory"/> — live on Windows, simulated elsewhere.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>Builds the fully-configured service provider.</summary>
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();
        Configure(services);
        return services.BuildServiceProvider();
    }

    /// <summary>Registers every service (kept public so tests can build the same graph).</summary>
    public static void Configure(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Hardware source: live on Windows, simulated elsewhere. One instance
        // implements both seams.
        services.AddSingleton<ISensorProvider>(_ => HardwareProviderFactory.Create());
        services.AddSingleton<IHardwareInfoProvider>(sp => (IHardwareInfoProvider)sp.GetRequiredService<ISensorProvider>());

        // Core engine + services.
        services.AddSingleton(sp => new MonitoringEngine(
            sp.GetRequiredService<ISensorProvider>(),
            new MonitoringOptions { Interval = TimeSpan.FromSeconds(1), HistoryCapacity = 60 }));
        services.AddSingleton<SnapshotService>();

        // App services.
        services.AddSingleton<Localizer>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<AppSettings>();
        services.AddSingleton<SettingsService>();

        // View-models.
        services.AddSingleton<MainWindowViewModel>();
    }
}
