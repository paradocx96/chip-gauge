using ChipGauge.App.Services;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// Presents a <see cref="HardwareItem"/>: identification rows, its sensors grouped
/// by kind, and nested devices. Sensors with no reading are hidden, and the visible
/// grouping is recomputed only when the set of readable sensors changes (so it
/// settles a second or two after startup and then stays stable).
/// </summary>
public sealed class HardwareItemViewModel : ObservableObject
{
    private readonly Localizer _localizer;
    private int _visibleSignature = -1;
    private IReadOnlyList<SensorGroupViewModel> _sensorGroups = [];
    private IReadOnlyList<SensorViewModel> _visibleSensors = [];

    public HardwareItemViewModel(HardwareItem item, HistoryStore history, Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(localizer);
        _localizer = localizer;

        Name = item.Name;
        Kind = item.Kind;
        Properties = [.. item.Properties.Select(kv => new PropertyRow(kv.Key, kv.Value))];
        Sensors = [.. item.Sensors.Select(s => new SensorViewModel(s, history.GetOrCreate(s.Id)))];
        SubItems = [.. item.SubHardware.Select(child => new HardwareItemViewModel(child, history, localizer))];

        RebuildGroups();
    }

    /// <summary>Device name.</summary>
    public string Name { get; }

    /// <summary>Device category.</summary>
    public HardwareKind Kind { get; }

    /// <summary>Static identification rows.</summary>
    public IReadOnlyList<PropertyRow> Properties { get; }

    /// <summary>The device's own sensors (including ones with no reading).</summary>
    public IReadOnlyList<SensorViewModel> Sensors { get; }

    /// <summary>Nested devices (e.g. per-core sensors).</summary>
    public IReadOnlyList<HardwareItemViewModel> SubItems { get; }

    /// <summary>Readable sensors grouped by kind, in display order.</summary>
    public IReadOnlyList<SensorGroupViewModel> SensorGroups => _sensorGroups;

    /// <summary>Readable sensors as a flat list (used for compact sub-device display).</summary>
    public IReadOnlyList<SensorViewModel> VisibleSensors => _visibleSensors;

    /// <summary>True when the device exposes any identification rows.</summary>
    public bool HasProperties => Properties.Count > 0;

    /// <summary>True when at least one sensor currently has a reading.</summary>
    public bool HasVisibleSensors => _sensorGroups.Count > 0;

    /// <summary>True when the device has anything worth showing (readings, identity, or sub-devices).</summary>
    public bool HasContent => HasVisibleSensors || HasProperties || SubItems.Any(s => s.HasContent);

    /// <summary>True when the device is detected but exposes nothing readable.</summary>
    public bool IsEmpty => !HasContent;

    /// <summary>Refreshes this device and every nested device, regrouping only when visibility changes.</summary>
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

        int signature = Sensors.Count(s => s.HasValue);
        if (signature != _visibleSignature)
        {
            RebuildGroups();
            OnPropertyChanged(nameof(SensorGroups));
            OnPropertyChanged(nameof(VisibleSensors));
            OnPropertyChanged(nameof(HasVisibleSensors));
            OnPropertyChanged(nameof(HasContent));
            OnPropertyChanged(nameof(IsEmpty));
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

    private void RebuildGroups()
    {
        _visibleSignature = Sensors.Count(s => s.HasValue);
        _visibleSensors = [.. Sensors.Where(s => s.HasValue)];
        _sensorGroups =
        [
            .. _visibleSensors
                .GroupBy(s => s.Kind)
                .OrderBy(g => SensorGroupViewModel.OrderFor(g.Key))
                .Select(g => new SensorGroupViewModel(_localizer, g.Key, [.. g])),
        ];
    }
}

/// <summary>A single identification key/value row.</summary>
/// <param name="Name">Property name.</param>
/// <param name="Value">Property value.</param>
public sealed record PropertyRow(string Name, string Value);
