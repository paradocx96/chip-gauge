namespace ChipGauge.Core.Models;

/// <summary>
/// A node in the hardware tree: a device (CPU, GPU, disk…) carrying static
/// identification <see cref="Properties"/>, its live <see cref="Sensors"/>, and
/// optionally nested <see cref="SubHardware"/> (for example per-core sensors
/// under a CPU package).
/// </summary>
public sealed class HardwareItem
{
    private readonly List<Sensor> _sensors = [];
    private readonly List<HardwareItem> _subHardware = [];
    private readonly Dictionary<string, string> _properties = new(StringComparer.Ordinal);

    /// <summary>Creates a hardware node.</summary>
    /// <param name="id">Stable, provider-unique identifier.</param>
    /// <param name="name">Human-readable device name.</param>
    /// <param name="kind">The device category.</param>
    public HardwareItem(string id, string name, HardwareKind kind)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(name);
        Id = id;
        Name = name;
        Kind = kind;
    }

    /// <summary>Stable identifier, unique within a provider.</summary>
    public string Id { get; }

    /// <summary>Human-readable device name (e.g. "AMD Ryzen 9 7950X").</summary>
    public string Name { get; }

    /// <summary>The device category.</summary>
    public HardwareKind Kind { get; }

    /// <summary>Static identification key/value pairs (model, vendor, firmware, capacities…).</summary>
    public IReadOnlyDictionary<string, string> Properties => _properties;

    /// <summary>The device's own sensors (not including sub-hardware sensors).</summary>
    public IReadOnlyList<Sensor> Sensors => _sensors;

    /// <summary>Nested devices (e.g. individual cores, memory modules).</summary>
    public IReadOnlyList<HardwareItem> SubHardware => _subHardware;

    /// <summary>Adds a sensor and returns this node for fluent construction.</summary>
    public HardwareItem AddSensor(Sensor sensor)
    {
        ArgumentNullException.ThrowIfNull(sensor);
        _sensors.Add(sensor);
        return this;
    }

    /// <summary>Adds a nested device and returns this node for fluent construction.</summary>
    public HardwareItem AddSubHardware(HardwareItem child)
    {
        ArgumentNullException.ThrowIfNull(child);
        _subHardware.Add(child);
        return this;
    }

    /// <summary>Sets an identification property and returns this node for fluent construction.</summary>
    public HardwareItem SetProperty(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _properties[key] = value ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Enumerates every sensor in this node and, recursively, in all sub-hardware —
    /// the flattened view the monitoring engine indexes.
    /// </summary>
    public IEnumerable<Sensor> EnumerateSensors()
    {
        foreach (Sensor sensor in _sensors)
        {
            yield return sensor;
        }

        foreach (HardwareItem child in _subHardware)
        {
            foreach (Sensor sensor in child.EnumerateSensors())
            {
                yield return sensor;
            }
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"{Name} ({Kind}) [{_sensors.Count} sensors]";
}
