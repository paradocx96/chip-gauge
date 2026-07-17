using ChipGauge.App.Services;
using ChipGauge.Core.Abstractions;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// The landing page: a summary card for the machine plus a wall of live tiles
/// (CPU, GPU, memory, each disk, network) — ChipGauge's at-a-glance overview.
/// </summary>
public sealed class DashboardViewModel : PageViewModel
{
    private readonly MonitoringEngine _engine;

    public DashboardViewModel(Localizer localizer, MonitoringEngine engine, IHardwareInfoProvider info)
        : base(localizer, "Nav_Dashboard", "dashboard")
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(info);
        _engine = engine;
        Summary = info.GetSystemSummary();
        Tiles = BuildTiles();
    }

    /// <summary>Machine summary shown in the header card.</summary>
    public SystemSummary Summary { get; }

    /// <summary>Human-readable uptime (e.g. "6h 30m"), or empty when unknown.</summary>
    public string UptimeText => Summary.Uptime is { } up
        ? $"{(int)up.TotalHours}h {up.Minutes}m"
        : string.Empty;

    /// <summary>The live tiles.</summary>
    public IReadOnlyList<DashboardTileViewModel> Tiles { get; }

    /// <inheritdoc />
    public override void OnTick()
    {
        foreach (DashboardTileViewModel tile in Tiles)
        {
            tile.Refresh();
        }
    }

    private List<DashboardTileViewModel> BuildTiles()
    {
        var tiles = new List<DashboardTileViewModel>();

        Add(tiles, "Nav_Cpu", "cpu", "cpu/load/total", "cpu/temp/package", "cpu/clock/core");
        Add(tiles, "Nav_Gpu", "gpu", "gpu/load/core", "gpu/temp/core", "gpu/power/board");
        Add(tiles, "Nav_Memory", "memory", "mem/load", "mem/used", null);

        foreach (HardwareItem disk in _engine.Hardware.Where(h => h.Kind == HardwareKind.Storage))
        {
            Add(tiles, disk.Name, "storage", $"{disk.Id}/activity", $"{disk.Id}/temp", null, literalTitle: true);
        }

        Add(tiles, "Nav_Network", "network", "net0/down", "net0/up", null);

        return tiles;
    }

    private void Add(
        List<DashboardTileViewModel> tiles,
        string titleKeyOrName,
        string iconKey,
        string primaryId,
        string? secondaryId,
        string? tertiaryId,
        bool literalTitle = false)
    {
        SensorViewModel? primary = Make(primaryId);
        if (primary is null)
        {
            return;
        }

        var secondary = new List<SensorViewModel>();
        if (Make(secondaryId) is { } s)
        {
            secondary.Add(s);
        }

        if (Make(tertiaryId) is { } t)
        {
            secondary.Add(t);
        }

        tiles.Add(new DashboardTileViewModel(Localizer, titleKeyOrName, iconKey, primary, secondary, literalTitle));
    }

    private SensorViewModel? Make(string? sensorId)
    {
        if (sensorId is not null && _engine.TryGetSensor(sensorId, out Sensor? sensor) && sensor is not null)
        {
            return new SensorViewModel(sensor, _engine.History.GetOrCreate(sensorId));
        }

        return null;
    }
}
