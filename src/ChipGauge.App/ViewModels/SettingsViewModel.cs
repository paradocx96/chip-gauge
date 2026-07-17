using ChipGauge.App.Services;

namespace ChipGauge.App.ViewModels;

/// <summary>Settings page: theme, language and polling interval.</summary>
public sealed class SettingsViewModel : PageViewModel
{
    private readonly ThemeService _theme;
    private readonly AppSettings _settings;

    public SettingsViewModel(Localizer localizer, ThemeService theme, AppSettings settings)
        : base(localizer, "Nav_Settings", "settings")
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(settings);
        _theme = theme;
        _settings = settings;
        _selectedLanguage = Localizer.Languages
            .FirstOrDefault(l => l.Code == localizer.CurrentLanguage) ?? Localizer.Languages[0];
    }

    private LanguageOption _selectedLanguage;

    /// <summary>Available theme options.</summary>
    public IReadOnlyList<AppTheme> Themes { get; } = [AppTheme.System, AppTheme.Light, AppTheme.Dark];

    /// <summary>The selected theme (applied immediately).</summary>
    public AppTheme SelectedTheme
    {
        get => _theme.Theme;
        set
        {
            if (_theme.Theme != value)
            {
                _theme.Theme = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Available languages.</summary>
    public IReadOnlyList<LanguageOption> Languages => Localizer.Languages;

    /// <summary>The selected language (applied immediately).</summary>
    public LanguageOption SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (value is not null && !ReferenceEquals(_selectedLanguage, value))
            {
                _selectedLanguage = value;
                Localizer.SetLanguage(value.Code);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Available temperature units.</summary>
    public IReadOnlyList<TemperatureUnit> TemperatureUnits { get; } =
        [TemperatureUnit.Celsius, TemperatureUnit.Fahrenheit];

    /// <summary>The selected temperature unit.</summary>
    public TemperatureUnit SelectedTemperatureUnit
    {
        get => _settings.TemperatureUnit;
        set
        {
            if (_settings.TemperatureUnit != value)
            {
                _settings.TemperatureUnit = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Polling interval in seconds (0.5–10).</summary>
    public double PollingSeconds
    {
        get => _settings.PollingSeconds;
        set
        {
            double clamped = Math.Clamp(value, 0.5, 10);
            if (Math.Abs(_settings.PollingSeconds - clamped) > double.Epsilon)
            {
                _settings.PollingSeconds = clamped;
                OnPropertyChanged();
            }
        }
    }

    /// <inheritdoc />
    public override void OnTick()
    {
        // Static page — nothing to refresh per tick.
    }
}
