using ChipGauge.App.Services;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// The HWiNFO-style flat sensor list: every sensor across all devices with its
/// current / min / max / average, filterable by a search term.
/// </summary>
public sealed partial class SensorsViewModel : PageViewModel
{
    private readonly IReadOnlyList<SensorRow> _all;

    [ObservableProperty]
    private string _filter = string.Empty;

    public SensorsViewModel(Localizer localizer, MonitoringEngine engine)
        : base(localizer, "Nav_Sensors", "sensors")
    {
        ArgumentNullException.ThrowIfNull(engine);

        var rows = new List<SensorRow>();
        foreach (HardwareItem device in engine.Hardware)
        {
            foreach (Sensor sensor in device.EnumerateSensors())
            {
                rows.Add(new SensorRow(device.Name, new SensorViewModel(sensor, engine.History.GetOrCreate(sensor.Id))));
            }
        }

        _all = rows;
    }

    /// <summary>The rows matching the current <see cref="Filter"/>.</summary>
    public IEnumerable<SensorRow> Rows => string.IsNullOrWhiteSpace(Filter)
        ? _all
        : _all.Where(r =>
            r.Device.Contains(Filter, StringComparison.OrdinalIgnoreCase) ||
            r.Sensor.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc />
    public override void OnTick()
    {
        foreach (SensorRow row in _all)
        {
            row.Sensor.Refresh();
        }
    }

    partial void OnFilterChanged(string value) => OnPropertyChanged(nameof(Rows));
}

/// <summary>A row in the sensor table: the owning device name and the sensor.</summary>
/// <param name="Device">The device the sensor belongs to.</param>
/// <param name="Sensor">The sensor view-model.</param>
public sealed record SensorRow(string Device, SensorViewModel Sensor);
