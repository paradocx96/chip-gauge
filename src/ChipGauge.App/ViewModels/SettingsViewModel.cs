using ChipGauge.App.Services;
using ChipGauge.Core.Abstractions;
using ChipGauge.Core.Monitoring;
using ChipGauge.Core.Reporting;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// Settings page. Each change is applied immediately (theme, language, polling
/// interval, temperature unit) and persisted via <see cref="SettingsService"/>.
/// </summary>
public sealed class SettingsViewModel : PageViewModel
{
    private readonly ThemeService _theme;
    private readonly AppSettings _settings;
    private readonly MonitoringEngine _engine;
    private readonly SettingsService _settingsService;
    private readonly SnapshotService _snapshots;
    private readonly IHardwareInfoProvider _info;
    private LanguageOption _selectedLanguage;
    private string? _lastExportPath;

    public SettingsViewModel(
        Localizer localizer,
        ThemeService theme,
        AppSettings settings,
        MonitoringEngine engine,
        SettingsService settingsService,
        SnapshotService snapshots,
        IHardwareInfoProvider info)
        : base(localizer, "Nav_Settings", "settings")
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(info);
        _theme = theme;
        _settings = settings;
        _engine = engine;
        _settingsService = settingsService;
        _snapshots = snapshots;
        _info = info;
        _selectedLanguage = Localizer.Languages.FirstOrDefault(l => l.Code == settings.Language)
            ?? Localizer.Languages[0];
    }

    /// <summary>Available theme options.</summary>
    public IReadOnlyList<AppTheme> Themes { get; } = [AppTheme.System, AppTheme.Light, AppTheme.Dark];

    /// <summary>The selected theme (applied and persisted immediately).</summary>
    public AppTheme SelectedTheme
    {
        get => _settings.Theme;
        set
        {
            if (_settings.Theme != value)
            {
                _settings.Theme = value;
                _theme.Theme = value;
                Persist();
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Available languages.</summary>
    public IReadOnlyList<LanguageOption> Languages => Localizer.Languages;

    /// <summary>The selected language (applied and persisted immediately).</summary>
    public LanguageOption SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (value is not null && !ReferenceEquals(_selectedLanguage, value))
            {
                _selectedLanguage = value;
                _settings.Language = value.Code;
                Localizer.SetLanguage(value.Code);
                Persist();
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Available temperature units.</summary>
    public IReadOnlyList<TemperatureUnit> TemperatureUnits { get; } =
        [TemperatureUnit.Celsius, TemperatureUnit.Fahrenheit];

    /// <summary>The selected temperature unit (applied and persisted immediately).</summary>
    public TemperatureUnit SelectedTemperatureUnit
    {
        get => _settings.TemperatureUnit;
        set
        {
            if (_settings.TemperatureUnit != value)
            {
                _settings.TemperatureUnit = value;
                DisplaySettings.TemperatureUnit = value;
                Persist();
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Polling interval in seconds (0.5–10), applied to the live engine.</summary>
    public double PollingSeconds
    {
        get => _settings.PollingSeconds;
        set
        {
            double clamped = Math.Clamp(value, 0.5, 10);
            if (Math.Abs(_settings.PollingSeconds - clamped) > double.Epsilon)
            {
                _settings.PollingSeconds = clamped;
                _engine.Interval = TimeSpan.FromSeconds(clamped);
                Persist();
                OnPropertyChanged();
            }
        }
    }

    /// <summary>The path of the most recently exported snapshot, or null.</summary>
    public string? LastExportPath
    {
        get => _lastExportPath;
        set
        {
            if (SetProperty(ref _lastExportPath, value))
            {
                OnPropertyChanged(nameof(HasExported));
            }
        }
    }

    /// <summary>True once a snapshot has been exported this session.</summary>
    public bool HasExported => !string.IsNullOrEmpty(_lastExportPath);

    /// <summary>A default file name for an exported snapshot.</summary>
    public static string SuggestedSnapshotFileName =>
        $"ChipGauge-Snapshot-{DateTime.Now:yyyyMMdd-HHmmss}.txt";

    /// <summary>Builds the plain-text system snapshot report (used by the export flow).</summary>
    public string BuildSnapshotReport() =>
        _snapshots.CreateTextReport(_engine.Hardware, _info.GetSystemSummary(), _info.GetSmartDrives());

    /// <inheritdoc />
    public override void OnTick()
    {
        // Static page — nothing to refresh per tick.
    }

    private void Persist() => _settingsService.Save(_settings);
}
