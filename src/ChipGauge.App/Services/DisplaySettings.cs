namespace ChipGauge.App.Services;

/// <summary>
/// Ambient display preferences read by value formatters (analogous to
/// <see cref="System.Globalization.CultureInfo.CurrentCulture"/>). Updated by the
/// settings layer so every sensor readout reflects the choice without threading the
/// setting through every view-model.
/// </summary>
public static class DisplaySettings
{
    /// <summary>The unit temperatures are displayed in.</summary>
    public static TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;
}
