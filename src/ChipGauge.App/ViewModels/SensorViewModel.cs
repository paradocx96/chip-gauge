using System.Globalization;
using ChipGauge.App.Services;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// Presents one <see cref="Sensor"/> and its rolling <see cref="SensorHistory"/> to
/// the UI: the current value, min/max/average, and the value series a chart plots.
/// The underlying sensor is updated by the engine on a background thread; the view
/// pulls fresh values on <see cref="Refresh"/> (invoked on the UI thread each tick).
/// </summary>
public sealed class SensorViewModel : ObservableObject
{
    private readonly Sensor _sensor;
    private readonly SensorHistory _history;

    public SensorViewModel(Sensor sensor, SensorHistory history)
    {
        ArgumentNullException.ThrowIfNull(sensor);
        ArgumentNullException.ThrowIfNull(history);
        _sensor = sensor;
        _history = history;
    }

    /// <summary>Sensor id.</summary>
    public string Id => _sensor.Id;

    /// <summary>Human-readable sensor name.</summary>
    public string Name => _sensor.Name;

    /// <summary>Unit symbol.</summary>
    public string Unit => _sensor.Unit;

    /// <summary>The measured quantity.</summary>
    public SensorKind Kind => _sensor.Kind;

    /// <summary>True when this sensor is naturally bounded 0–100 (fixed chart axis).</summary>
    public bool IsPercentage => _sensor.Kind.IsPercentage();

    /// <summary>True once the sensor has produced at least one reading.</summary>
    public bool HasValue => _sensor.Value is not null;

    /// <summary>The current numeric value (0 when never sampled).</summary>
    public float Numeric => _sensor.Value ?? 0f;

    /// <summary>Formatted current value with unit.</summary>
    public string Value => Format(_sensor.Value);

    /// <summary>Formatted running minimum.</summary>
    public string Min => Format(_sensor.Min);

    /// <summary>Formatted running maximum.</summary>
    public string Max => Format(_sensor.Max);

    /// <summary>Formatted running average.</summary>
    public string Average => Format(_sensor.Average);

    /// <summary>The value series, oldest first — what the chart control plots.</summary>
    public float[] Values => _history.ToValueArray();

    /// <summary>Re-reads the sensor and raises change notifications for bound members.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Numeric));
        OnPropertyChanged(nameof(HasValue));
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(Min));
        OnPropertyChanged(nameof(Max));
        OnPropertyChanged(nameof(Average));
        OnPropertyChanged(nameof(Values));
    }

    private string Format(float? value)
    {
        if (value is not { } v)
        {
            return "—";
        }

        if (Kind == SensorKind.Throughput)
        {
            return FormatThroughput(v);
        }

        if (Kind == SensorKind.Temperature && DisplaySettings.TemperatureUnit == TemperatureUnit.Fahrenheit)
        {
            return string.Create(CultureInfo.CurrentCulture, $"{(v * 9f / 5f) + 32f:0.#} °F");
        }

        return string.Create(CultureInfo.CurrentCulture, $"{v:0.#} {Unit}").TrimEnd();
    }

    private static string FormatThroughput(float bytesPerSecond)
    {
        string[] units = ["B/s", "KB/s", "MB/s", "GB/s"];
        double value = bytesPerSecond;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return string.Create(CultureInfo.CurrentCulture, $"{value:0.#} {units[unit]}");
    }
}
