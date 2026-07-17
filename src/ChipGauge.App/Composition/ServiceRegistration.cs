using ChipGauge.App.Services;
using ChipGauge.App.ViewModels;
using ChipGauge.Core.Abstractions;
using ChipGauge.Core.Monitoring;
using ChipGauge.Core.Reporting;
using ChipGauge.Hardware.Mock;
using Microsoft.Extensions.DependencyInjection;

namespace ChipGauge.App.Composition;

/// <summary>
/// The application's composition root. Phase 1 binds the UI to the deterministic
/// <see cref="MockSensorProvider"/>; swapping in the Windows provider is a
/// one-line change here once it lands.
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

        // Hardware source (simulated for Phase 1). One instance implements both seams.
        services.AddSingleton<MockSensorProvider>();
        services.AddSingleton<ISensorProvider>(sp => sp.GetRequiredService<MockSensorProvider>());
        services.AddSingleton<IHardwareInfoProvider>(sp => sp.GetRequiredService<MockSensorProvider>());

        // Core engine + services.
        services.AddSingleton(sp => new MonitoringEngine(
            sp.GetRequiredService<ISensorProvider>(),
            new MonitoringOptions { Interval = TimeSpan.FromSeconds(1), HistoryCapacity = 60 }));
        services.AddSingleton<SnapshotService>();

        // App services.
        services.AddSingleton<Localizer>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<AppSettings>();

        // View-models.
        services.AddSingleton<MainWindowViewModel>();
    }
}
