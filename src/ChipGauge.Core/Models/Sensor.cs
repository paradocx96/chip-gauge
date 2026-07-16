using System.Globalization;

namespace ChipGauge.Core.Models;

/// <summary>
/// A single measurable channel on a piece of hardware (e.g. "CPU Package"
/// temperature). Holds the latest value plus running minimum, maximum and
/// average since the last statistics reset — the min/max/avg columns that
/// power users expect from HWiNFO.
/// </summary>
public sealed class Sensor
{
    private double _sum;
    private long _count;

    /// <summary>Creates a sensor descriptor.</summary>
    /// <param name="id">Stable, provider-unique identifier.</param>
    /// <param name="name">Human-readable name (localizable at the UI layer).</param>
    /// <param name="kind">The quantity measured.</param>
    /// <param name="unit">Unit symbol; defaults to the kind's canonical unit when null.</param>
    public Sensor(string id, string name, SensorKind kind, string? unit = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(name);
        Id = id;
        Name = name;
        Kind = kind;
        Unit = string.IsNullOrEmpty(unit) ? kind.DefaultUnit() : unit;
    }

    /// <summary>Stable identifier, unique within a provider.</summary>
    public string Id { get; }

    /// <summary>Human-readable name.</summary>
    public string Name { get; }

    /// <summary>The physical quantity measured.</summary>
    public SensorKind Kind { get; }

    /// <summary>Unit symbol (e.g. "°C", "MHz", "%").</summary>
    public string Unit { get; }

    /// <summary>The most recently recorded value, or <see langword="null"/> before the first sample.</summary>
    public float? Value { get; private set; }

    /// <summary>The lowest value observed since the last reset.</summary>
    public float? Min { get; private set; }

    /// <summary>The highest value observed since the last reset.</summary>
    public float? Max { get; private set; }

    /// <summary>The number of samples folded into the running statistics.</summary>
    public long SampleCount => _count;

    /// <summary>The arithmetic mean of all values since the last reset, or <see langword="null"/> when no samples exist.</summary>
    public float? Average => _count > 0 ? (float)(_sum / _count) : null;

    /// <summary>Records a new value and folds it into the running min/max/average.</summary>
    public void Update(float value)
    {
        Value = value;
        Min = Min is null ? value : MathF.Min(Min.Value, value);
        Max = Max is null ? value : MathF.Max(Max.Value, value);
        _sum += value;
        _count++;
    }

    /// <summary>
    /// Clears the running statistics, re-seeding them from the current value
    /// (matching the "reset min/max" behavior of desktop monitors).
    /// </summary>
    public void ResetStatistics()
    {
        if (Value is { } v)
        {
            Min = Max = v;
            _sum = v;
            _count = 1;
        }
        else
        {
            Min = Max = null;
            _sum = 0d;
            _count = 0;
        }
    }

    /// <summary>Formats the current value with its unit using the invariant culture.</summary>
    public string FormatValue() => Value is { } v
        ? string.Create(CultureInfo.InvariantCulture, $"{v:0.###} {Unit}").TrimEnd()
        : "—";

    /// <inheritdoc />
    public override string ToString() => $"{Name} ({Kind}) = {FormatValue()}";
}
