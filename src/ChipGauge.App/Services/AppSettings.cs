using CommunityToolkit.Mvvm.ComponentModel;

namespace ChipGauge.App.Services;

/// <summary>
/// User preferences, persisted by <see cref="SettingsService"/> to
/// <c>%AppData%/ChipGauge/settings.json</c>.
/// </summary>
public sealed partial class AppSettings : ObservableObject
{
    /// <summary>Theme preference.</summary>
    [ObservableProperty]
    private AppTheme _theme = AppTheme.System;

    /// <summary>UI language code (e.g. "en", "de").</summary>
    [ObservableProperty]
    private string _language = "en";

    /// <summary>Preferred temperature unit.</summary>
    [ObservableProperty]
    private TemperatureUnit _temperatureUnit = TemperatureUnit.Celsius;

    /// <summary>Sampling interval in seconds (0.5–10).</summary>
    [ObservableProperty]
    private double _pollingSeconds = 1.0;
}

/// <summary>Temperature display unit.</summary>
public enum TemperatureUnit
{
    /// <summary>Degrees Celsius.</summary>
    Celsius,

    /// <summary>Degrees Fahrenheit.</summary>
    Fahrenheit,
}
