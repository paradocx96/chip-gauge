namespace ChipGauge.Core.Models;

/// <summary>
/// The physical quantity a <see cref="Sensor"/> measures. Drives the default unit,
/// formatting, and the color/scale a chart uses to render the sensor.
/// </summary>
public enum SensorKind
{
    /// <summary>Unclassified reading.</summary>
    Unknown = 0,

    /// <summary>Utilization / load, as a percentage (0–100 %).</summary>
    Load,

    /// <summary>Clock frequency (MHz).</summary>
    Clock,

    /// <summary>Temperature (°C).</summary>
    Temperature,

    /// <summary>Voltage (V).</summary>
    Voltage,

    /// <summary>Electric current (A).</summary>
    Current,

    /// <summary>Power draw (W).</summary>
    Power,

    /// <summary>Fan / pump rotational speed (RPM).</summary>
    Fan,

    /// <summary>Liquid flow rate (L/h).</summary>
    Flow,

    /// <summary>Data rate / throughput (bytes per second, network or disk).</summary>
    Throughput,

    /// <summary>Fill level such as memory or VRAM usage (percentage or absolute).</summary>
    Level,

    /// <summary>Absolute data amount (bytes: memory used, VRAM, host writes).</summary>
    Data,

    /// <summary>A control signal such as a fan duty cycle (percentage).</summary>
    Control,

    /// <summary>A dimensionless factor or multiplier.</summary>
    Factor,
}

/// <summary>Helpers describing the default presentation of a <see cref="SensorKind"/>.</summary>
public static class SensorKindExtensions
{
    /// <summary>Returns the canonical unit symbol for a sensor kind (e.g. "°C", "MHz").</summary>
    public static string DefaultUnit(this SensorKind kind) => kind switch
    {
        SensorKind.Load => "%",
        SensorKind.Clock => "MHz",
        SensorKind.Temperature => "°C",
        SensorKind.Voltage => "V",
        SensorKind.Current => "A",
        SensorKind.Power => "W",
        SensorKind.Fan => "RPM",
        SensorKind.Flow => "L/h",
        SensorKind.Throughput => "B/s",
        SensorKind.Level => "%",
        SensorKind.Data => "B",
        SensorKind.Control => "%",
        SensorKind.Factor => "x",
        _ => string.Empty,
    };

    /// <summary>
    /// Returns true when the sensor is naturally bounded to the 0–100 range,
    /// letting charts use a fixed axis instead of auto-scaling.
    /// </summary>
    public static bool IsPercentage(this SensorKind kind) =>
        kind is SensorKind.Load or SensorKind.Level or SensorKind.Control;
}
