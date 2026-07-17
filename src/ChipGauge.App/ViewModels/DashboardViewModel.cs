using ChipGauge.App.Services;
using ChipGauge.Core.Abstractions;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// The landing page: a summary card for the machine plus a wall of live tiles
/// (CPU, GPU, memory, each disk, network). Tiles are built by picking sensors from
/// the discovered hardware tree <em>by kind</em> (not by hard-coded id), so the same
/// dashboard works with the simulated provider and the live Windows provider.
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

        if (FirstOf(HardwareKind.Cpu) is { } cpu)
        {
            AddTile(tiles, "Nav_Cpu", "cpu", literalTitle: false,
                primary: Pick(cpu, SensorKind.Load, "Total", "CPU Total"),
                secondary1: Pick(cpu, SensorKind.Temperature, "Package", "CPU Package", "Core"),
                secondary2: Pick(cpu, SensorKind.Power, "Package") ?? Pick(cpu, SensorKind.Clock, "Core"));
        }

        if (FirstOf(HardwareKind.Gpu) is { } gpu)
        {
            AddTile(tiles, "Nav_Gpu", "gpu", literalTitle: false,
                primary: Pick(gpu, SensorKind.Load, "3D", "Core", "GPU"),
                secondary1: Pick(gpu, SensorKind.Temperature),
                secondary2: Pick(gpu, SensorKind.Power));
        }

        if (PreferPhysicalMemory() is { } memory)
        {
            AddTile(tiles, "Nav_Memory", "memory", literalTitle: false,
                primary: Pick(memory, SensorKind.Load, "Memory") ?? Pick(memory, SensorKind.Level, "Memory"),
                secondary1: Pick(memory, SensorKind.Data, "Used"),
                secondary2: null);
        }

        foreach (HardwareItem disk in AllOf(HardwareKind.Storage))
        {
            AddTile(tiles, disk.Name, "storage", literalTitle: true,
                primary: Pick(disk, SensorKind.Load) ?? Pick(disk, SensorKind.Level) ?? Pick(disk, SensorKind.Temperature),
                secondary1: Pick(disk, SensorKind.Temperature),
                secondary2: null);
        }

        foreach (HardwareItem net in AllOf(HardwareKind.Network))
        {
            AddTile(tiles, net.Name, "network", literalTitle: true,
                primary: Pick(net, SensorKind.Throughput, "Download", "Down", "Receive") ?? Pick(net, SensorKind.Throughput),
                secondary1: Pick(net, SensorKind.Throughput, "Upload", "Up", "Send"),
                secondary2: null);
        }

        return tiles;
    }

    private void AddTile(
        List<DashboardTileViewModel> tiles,
        string titleKeyOrName,
        string iconKey,
        bool literalTitle,
        Sensor? primary,
        Sensor? secondary1,
        Sensor? secondary2)
    {
        if (primary is null)
        {
            return;
        }

        var secondaries = new List<SensorViewModel>();
        if (secondary1 is not null && secondary1.Id != primary.Id)
        {
            secondaries.Add(Make(secondary1));
        }

        if (secondary2 is not null && secondary2.Id != primary.Id)
        {
            secondaries.Add(Make(secondary2));
        }

        tiles.Add(new DashboardTileViewModel(Localizer, titleKeyOrName, iconKey, Make(primary), secondaries, literalTitle));
    }

    private HardwareItem? FirstOf(HardwareKind kind) =>
        _engine.Hardware.FirstOrDefault(h => h.Kind == kind);

    private IEnumerable<HardwareItem> AllOf(HardwareKind kind) =>
        _engine.Hardware.Where(h => h.Kind == kind);

    // Prefer a physical-memory device over a "Virtual Memory" one for the tile.
    private HardwareItem? PreferPhysicalMemory() =>
        _engine.Hardware
            .Where(h => h.Kind == HardwareKind.Memory)
            .OrderBy(h => h.Name.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .FirstOrDefault();

    private static Sensor? Pick(HardwareItem hardware, SensorKind kind, params string[] nameHints)
    {
        List<Sensor> candidates = [.. hardware.EnumerateSensors().Where(s => s.Kind == kind)];
        foreach (string hint in nameHints)
        {
            Sensor? match = candidates.FirstOrDefault(s => s.Name.Contains(hint, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }
        }

        return candidates.FirstOrDefault();
    }

    private SensorViewModel Make(Sensor sensor) =>
        new(sensor, _engine.History.GetOrCreate(sensor.Id));
}
