using CommunityToolkit.Mvvm.ComponentModel;

namespace ChipGauge.App.Services;

/// <summary>
/// In-memory application settings. Phase 1 keeps these transient; a later phase
/// will persist them to <c>%AppData%/ChipGauge/settings.json</c>.
/// </summary>
public sealed partial class AppSettings : ObservableObject
{
    /// <summary>Preferred temperature unit.</summary>
    [ObservableProperty]
    private TemperatureUnit _temperatureUnit = TemperatureUnit.Celsius;

    /// <summary>Sampling interval in seconds shown in the UI.</summary>
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
