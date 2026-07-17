using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;

namespace ChipGauge.App.ViewModels;

/// <summary>Presents a <see cref="HardwareItem"/>: identification rows, its sensors, and nested devices.</summary>
public sealed class HardwareItemViewModel
{
    public HardwareItemViewModel(HardwareItem item, HistoryStore history)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(history);

        Name = item.Name;
        Kind = item.Kind;
        Properties = [.. item.Properties.Select(kv => new PropertyRow(kv.Key, kv.Value))];
        Sensors = [.. item.Sensors.Select(s => new SensorViewModel(s, history.GetOrCreate(s.Id)))];
        SubItems = [.. item.SubHardware.Select(child => new HardwareItemViewModel(child, history))];
    }

    /// <summary>Device name.</summary>
    public string Name { get; }

    /// <summary>Device category.</summary>
    public HardwareKind Kind { get; }

    /// <summary>Static identification rows.</summary>
    public IReadOnlyList<PropertyRow> Properties { get; }

    /// <summary>The device's own sensors.</summary>
    public IReadOnlyList<SensorViewModel> Sensors { get; }

    /// <summary>Nested devices (e.g. per-core sensors).</summary>
    public IReadOnlyList<HardwareItemViewModel> SubItems { get; }

    /// <summary>True when the device exposes any identification rows.</summary>
    public bool HasProperties => Properties.Count > 0;

    /// <summary>True when the device exposes any sensors.</summary>
    public bool HasSensors => Sensors.Count > 0;

    /// <summary>Refreshes this device and every nested device.</summary>
    public void Refresh()
    {
        foreach (SensorViewModel sensor in Sensors)
        {
            sensor.Refresh();
        }

        foreach (HardwareItemViewModel child in SubItems)
        {
            child.Refresh();
        }
    }

    /// <summary>Enumerates this device's sensors and those of all nested devices.</summary>
    public IEnumerable<SensorViewModel> EnumerateSensors()
    {
        foreach (SensorViewModel sensor in Sensors)
        {
            yield return sensor;
        }

        foreach (HardwareItemViewModel child in SubItems)
        {
            foreach (SensorViewModel sensor in child.EnumerateSensors())
            {
                yield return sensor;
            }
        }
    }
}

/// <summary>A single identification key/value row.</summary>
/// <param name="Name">Property name.</param>
/// <param name="Value">Property value.</param>
public sealed record PropertyRow(string Name, string Value);
