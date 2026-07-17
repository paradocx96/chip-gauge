using ChipGauge.App.Services;
using ChipGauge.Core.Abstractions;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;
using ChipGauge.Core.Reporting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// Root view-model: owns the navigation pages, the current selection, and the
/// top-bar theme toggle. Receives the per-second tick and forwards it to the
/// visible page.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private PageViewModel _selectedPage;

    public MainWindowViewModel(
        Localizer localizer,
        ThemeService theme,
        AppSettings settings,
        SettingsService settingsService,
        MonitoringEngine engine,
        IHardwareInfoProvider info,
        ISensorProvider provider,
        SnapshotService snapshots)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(snapshots);

        Localizer = localizer;
        Theme = theme;
        IsSimulated = provider.IsSimulated;
        ShowElevationNotice = provider.RequiresElevation && !provider.IsElevated;

        Pages =
        [
            new DashboardViewModel(localizer, engine, info),
            new CategoryViewModel(localizer, "Nav_Cpu", "cpu", HardwareKind.Cpu, engine),
            new CategoryViewModel(localizer, "Nav_Gpu", "gpu", HardwareKind.Gpu, engine),
            new CategoryViewModel(localizer, "Nav_Memory", "memory", HardwareKind.Memory, engine),
            new CategoryViewModel(localizer, "Nav_Storage", "storage", HardwareKind.Storage, engine),
            new CategoryViewModel(localizer, "Nav_Network", "network", HardwareKind.Network, engine),
            new CategoryViewModel(localizer, "Nav_Mainboard", "mainboard", HardwareKind.Motherboard, engine),
            new SensorsViewModel(localizer, engine),
            new SettingsViewModel(localizer, theme, settings, engine, settingsService, snapshots, info),
        ];

        _selectedPage = Pages[0];
    }

    /// <summary>The shared localizer (bound by the view for the window title etc.).</summary>
    public Localizer Localizer { get; }

    /// <summary>The theme service (bound by the top-bar toggle).</summary>
    public ThemeService Theme { get; }

    /// <summary>The navigation pages.</summary>
    public IReadOnlyList<PageViewModel> Pages { get; }

    /// <summary>Product name shown in the sidebar header.</summary>
    public string AppName => "ChipGauge";

    /// <summary>True when the data is simulated (drives the "demo data" badge).</summary>
    public bool IsSimulated { get; }

    /// <summary>True when the app is running without the elevation needed for full sensors.</summary>
    public bool ShowElevationNotice { get; }

    /// <summary>Forwards a tick to the visible page.</summary>
    public void OnTick() => SelectedPage.OnTick();

    /// <summary>Cycles System → Light → Dark for the quick top-bar toggle.</summary>
    [RelayCommand]
    private void CycleTheme() => Theme.Theme = Theme.Theme switch
    {
        AppTheme.System => AppTheme.Light,
        AppTheme.Light => AppTheme.Dark,
        _ => AppTheme.System,
    };

    partial void OnSelectedPageChanged(PageViewModel value) => value.OnTick();
}
