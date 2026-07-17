using ChipGauge.Core.Models;
using LibreHardwareMonitor.Hardware;

namespace ChipGauge.Hardware.Windows;

/// <summary>The ChipGauge sensor kind and unit for a LibreHardwareMonitor sensor.</summary>
/// <param name="Kind">The mapped <see cref="SensorKind"/>.</param>
/// <param name="Unit">The unit symbol LibreHardwareMonitor reports values in.</param>
public readonly record struct SensorMapping(SensorKind Kind, string Unit);

/// <summary>Maps LibreHardwareMonitor <see cref="SensorType"/> values onto ChipGauge sensor kinds and units.</summary>
public static class SensorTypeMapper
{
    /// <summary>
    /// Returns the <see cref="SensorKind"/> and unit for a LibreHardwareMonitor sensor type.
    /// Units follow LibreHardwareMonitor's conventions (e.g. <see cref="SensorType.Data"/> is GB,
    /// <see cref="SensorType.SmallData"/> is MB, <see cref="SensorType.Throughput"/> is bytes/second).
    /// </summary>
    public static SensorMapping Map(SensorType type) => type switch
    {
        SensorType.Voltage => new(SensorKind.Voltage, "V"),
        SensorType.Current => new(SensorKind.Current, "A"),
        SensorType.Power => new(SensorKind.Power, "W"),
        SensorType.Clock => new(SensorKind.Clock, "MHz"),
        SensorType.Frequency => new(SensorKind.Clock, "Hz"),
        SensorType.Temperature => new(SensorKind.Temperature, "°C"),
        SensorType.Load => new(SensorKind.Load, "%"),
        SensorType.Fan => new(SensorKind.Fan, "RPM"),
        SensorType.Flow => new(SensorKind.Flow, "L/h"),
        SensorType.Control => new(SensorKind.Control, "%"),
        SensorType.Level => new(SensorKind.Level, "%"),
        SensorType.Factor => new(SensorKind.Factor, "x"),
        SensorType.Data => new(SensorKind.Data, "GB"),
        SensorType.SmallData => new(SensorKind.Data, "MB"),
        SensorType.Throughput => new(SensorKind.Throughput, "B/s"),
        SensorType.Energy => new(SensorKind.Data, "mWh"),
        SensorType.Humidity => new(SensorKind.Level, "%"),
        _ => new(SensorKind.Unknown, string.Empty),
    };
}
