using System.Globalization;
using ChipGauge.Core.Abstractions;
using ChipGauge.Core.Models;
using ChipGauge.Core.Storage;

namespace ChipGauge.Hardware.Mock;

/// <summary>
/// A fully simulated <see cref="ISensorProvider"/> and <see cref="IHardwareInfoProvider"/>.
/// It fabricates a plausible desktop — CPU with per-core sensors, GPU, memory,
/// two drives, a NIC and a mainboard — and drives every sensor with a
/// deterministic waveform. Given the same seed it always produces identical data,
/// which makes it ideal for UI development, screenshots, demos and CI, none of
/// which can rely on real (elevated) hardware access.
/// </summary>
public sealed class MockSensorProvider : ISensorProvider, IHardwareInfoProvider
{
    private readonly List<HardwareItem> _hardware = [];
    private readonly List<string> _sensorIds = [];
    private readonly Dictionary<string, Signal> _signals = new(StringComparer.Ordinal);
    private readonly List<SmartDrive> _drives = [];
    private readonly SystemSummary _summary;
    private readonly int _seed;
    private int _channel;
    private long _tick;

    /// <summary>Creates the simulated machine.</summary>
    /// <param name="seed">Seed controlling the (reproducible) noise pattern.</param>
    /// <param name="coreCount">Number of simulated CPU cores.</param>
    public MockSensorProvider(int seed = 1337, int coreCount = 8)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(coreCount);
        _seed = seed;

        BuildCpu(coreCount);
        BuildGpu();
        BuildMemory();
        BuildStorage();
        BuildNetwork();
        BuildMotherboard();
        _summary = BuildSummary();
    }

    /// <inheritdoc />
    public string Name => "ChipGauge Mock Provider";

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public bool RequiresElevation => false;

    /// <inheritdoc />
    public bool IsSimulated => true;

    /// <summary>The number of ticks (sampling passes) produced so far.</summary>
    public long Tick => _tick;

    /// <inheritdoc />
    public IReadOnlyList<HardwareItem> GetHardware() => _hardware;

    /// <inheritdoc />
    public IReadOnlyList<SensorReading> Sample()
    {
        long tick = ++_tick;
        var readings = new List<SensorReading>(_sensorIds.Count);
        foreach (string id in _sensorIds)
        {
            readings.Add(new SensorReading(id, _signals[id].ValueAt(tick)));
        }

        return readings;
    }

    /// <inheritdoc />
    public SystemSummary GetSystemSummary() => _summary;

    /// <inheritdoc />
    public IReadOnlyList<SmartDrive> GetSmartDrives() => _drives;

    /// <inheritdoc />
    public void Dispose() => GC.SuppressFinalize(this);

    private void BuildCpu(int coreCount)
    {
        var cpu = new HardwareItem("cpu", "AMD Ryzen 9 7900X", HardwareKind.Cpu)
            .SetProperty("Codename", "Raphael")
            .SetProperty("Cores", coreCount.ToString(CultureInfo.InvariantCulture))
            .SetProperty("Threads", (coreCount * 2).ToString(CultureInfo.InvariantCulture))
            .SetProperty("Socket", "AM5 (LGA 1718)")
            .SetProperty("Lithography", "5 nm")
            .SetProperty("Base Clock", "4.7 GHz")
            .SetProperty("Max Boost", "5.6 GHz")
            .SetProperty("TDP", "170 W")
            .SetProperty("L1 Cache", "768 KB")
            .SetProperty("L2 Cache", "12 MB")
            .SetProperty("L3 Cache", "64 MB")
            .SetProperty("Instruction Sets", "MMX, SSE4.2, AVX2, AVX-512, FMA3");

        AddSensor(cpu, "cpu/load/total", "CPU Total Load", SensorKind.Load, 32f, 26f, 40f, 6f, 2f, 100f);
        AddSensor(cpu, "cpu/clock/core", "Core Clock", SensorKind.Clock, 4800f, 700f, 40f, 60f, 3500f, 5600f);
        AddSensor(cpu, "cpu/temp/package", "CPU Package", SensorKind.Temperature, 58f, 18f, 45f, 2f, 30f, 95f);
        AddSensor(cpu, "cpu/power/package", "Package Power", SensorKind.Power, 85f, 55f, 45f, 6f, 15f, 200f);
        AddSensor(cpu, "cpu/voltage/core", "Core Voltage", SensorKind.Voltage, 1.25f, 0.12f, 35f, 0.01f, 0.9f, 1.5f);

        for (var i = 0; i < coreCount; i++)
        {
            var core = new HardwareItem($"cpu/core{i}", $"Core #{i}", HardwareKind.Cpu);
            AddSensor(core, $"cpu/core{i}/load", $"Core #{i} Load", SensorKind.Load, 30f, 35f, 20f + i, 12f, 0f, 100f);
            AddSensor(core, $"cpu/core{i}/clock", $"Core #{i} Clock", SensorKind.Clock, 4700f, 800f, 25f + i, 80f, 3500f, 5600f);
            cpu.AddSubHardware(core);
        }

        _hardware.Add(cpu);
    }

    private void BuildGpu()
    {
        var gpu = new HardwareItem("gpu", "NVIDIA GeForce RTX 4070", HardwareKind.Gpu)
            .SetProperty("GPU", "AD104")
            .SetProperty("VBIOS", "95.04.2A.40.6C")
            .SetProperty("Driver", "551.86 (Studio)")
            .SetProperty("Bus Interface", "PCIe 4.0 x16")
            .SetProperty("Memory Size", "12 GB")
            .SetProperty("Memory Type", "GDDR6X")
            .SetProperty("Memory Bus", "192-bit");

        AddSensor(gpu, "gpu/load/core", "GPU Load", SensorKind.Load, 40f, 38f, 35f, 8f, 0f, 100f);
        AddSensor(gpu, "gpu/clock/core", "GPU Core Clock", SensorKind.Clock, 2610f, 200f, 35f, 20f, 300f, 2850f);
        AddSensor(gpu, "gpu/clock/memory", "Memory Clock", SensorKind.Clock, 10500f, 400f, 50f, 30f, 400f, 11000f);
        AddSensor(gpu, "gpu/temp/core", "GPU Temperature", SensorKind.Temperature, 62f, 12f, 50f, 2f, 30f, 90f);
        AddSensor(gpu, "gpu/temp/hotspot", "GPU Hotspot", SensorKind.Temperature, 74f, 12f, 50f, 2f, 35f, 100f);
        AddSensor(gpu, "gpu/fan", "GPU Fan", SensorKind.Fan, 1450f, 550f, 55f, 40f, 0f, 2800f);
        AddSensor(gpu, "gpu/vram/used", "VRAM Used", SensorKind.Data, 6.2f, 2.6f, 60f, 0.3f, 0.5f, 12f, "GB");
        AddSensor(gpu, "gpu/power/board", "Board Power", SensorKind.Power, 140f, 60f, 40f, 8f, 15f, 220f);

        _hardware.Add(gpu);
    }

    private void BuildMemory()
    {
        var memory = new HardwareItem("memory", "32 GB DDR5", HardwareKind.Memory)
            .SetProperty("Total", "32 GB")
            .SetProperty("Type", "DDR5")
            .SetProperty("Speed", "6000 MT/s")
            .SetProperty("Channels", "Dual")
            .SetProperty("Timings", "30-38-38-96")
            .SetProperty("Profile", "EXPO I");

        AddSensor(memory, "mem/load", "Memory Used", SensorKind.Level, 46f, 14f, 70f, 3f, 8f, 96f);
        AddSensor(memory, "mem/used", "Memory Used", SensorKind.Data, 14.7f, 4.5f, 70f, 0.8f, 2.5f, 31f, "GB");

        for (var i = 0; i < 2; i++)
        {
            memory.AddSubHardware(new HardwareItem($"memory/dimm{i}", $"DIMM {i + 1}", HardwareKind.Memory)
                .SetProperty("Capacity", "16 GB")
                .SetProperty("Manufacturer", "G.Skill")
                .SetProperty("Part Number", "F5-6000J3038F16G")
                .SetProperty("Rank", "1Rx8"));
        }

        _hardware.Add(memory);
    }

    private void BuildStorage()
    {
        var ssd = new HardwareItem("disk0", "Samsung SSD 990 PRO 2TB", HardwareKind.Storage)
            .SetProperty("Interface", "NVMe PCIe 4.0 x4")
            .SetProperty("Capacity", "2 TB")
            .SetProperty("Firmware", "4B2QJXD7")
            .SetProperty("Form Factor", "M.2 2280");
        AddSensor(ssd, "disk0/temp", "Drive Temperature", SensorKind.Temperature, 44f, 8f, 55f, 2f, 25f, 80f);
        AddSensor(ssd, "disk0/activity", "Activity", SensorKind.Load, 18f, 30f, 25f, 15f, 0f, 100f);
        // Throughput values are in bytes/second, matching LibreHardwareMonitor.
        AddSensor(ssd, "disk0/read", "Read Rate", SensorKind.Throughput, 3.8e8f, 6.2e8f, 22f, 1.2e8f, 0f, 7e9f);
        AddSensor(ssd, "disk0/write", "Write Rate", SensorKind.Throughput, 2.6e8f, 4.2e8f, 18f, 1.0e8f, 0f, 6.9e9f);
        _hardware.Add(ssd);

        var hdd = new HardwareItem("disk1", "Seagate BarraCuda 4TB", HardwareKind.Storage)
            .SetProperty("Interface", "SATA III 6 Gb/s")
            .SetProperty("Capacity", "4 TB")
            .SetProperty("Firmware", "CC43")
            .SetProperty("RPM", "5400");
        AddSensor(hdd, "disk1/temp", "Drive Temperature", SensorKind.Temperature, 38f, 6f, 65f, 1.5f, 22f, 65f);
        AddSensor(hdd, "disk1/activity", "Activity", SensorKind.Load, 8f, 16f, 30f, 10f, 0f, 100f);
        AddSensor(hdd, "disk1/read", "Read Rate", SensorKind.Throughput, 9.0e7f, 1.1e8f, 26f, 3.0e7f, 0f, 2.6e8f);
        AddSensor(hdd, "disk1/write", "Write Rate", SensorKind.Throughput, 7.0e7f, 9.0e7f, 24f, 2.5e7f, 0f, 2.5e8f);
        _hardware.Add(hdd);

        BuildSmartDrives();
    }

    private void BuildNetwork()
    {
        var nic = new HardwareItem("net0", "Intel Ethernet Controller I225-V", HardwareKind.Network)
            .SetProperty("Link Speed", "2.5 Gbps")
            .SetProperty("MAC Address", "A8:A1:59:3C:7D:20")
            .SetProperty("IPv4", "192.168.1.42")
            .SetProperty("Driver", "1.1.4.38");
        AddSensor(nic, "net0/down", "Download", SensorKind.Throughput, 1.2e7f, 2.2e7f, 18f, 8.0e6f, 0f, 3.0e8f);
        AddSensor(nic, "net0/up", "Upload", SensorKind.Throughput, 4.0e6f, 8.0e6f, 22f, 3.0e6f, 0f, 3.0e8f);
        _hardware.Add(nic);
    }

    private void BuildMotherboard()
    {
        var board = new HardwareItem("mb", "ASUS ROG STRIX X670E-E GAMING", HardwareKind.Motherboard)
            .SetProperty("Chipset", "AMD X670E")
            .SetProperty("BIOS Version", "2412")
            .SetProperty("BIOS Date", "2024-03-18")
            .SetProperty("Super I/O", "Nuvoton NCT6799D");
        AddSensor(board, "mb/temp/chipset", "Chipset Temperature", SensorKind.Temperature, 48f, 7f, 60f, 1.5f, 30f, 85f);
        AddSensor(board, "mb/temp/vrm", "VRM Temperature", SensorKind.Temperature, 52f, 10f, 55f, 2f, 30f, 100f);
        AddSensor(board, "mb/voltage/vcore", "CPU VDDCR", SensorKind.Voltage, 1.24f, 0.1f, 35f, 0.01f, 0.9f, 1.5f);
        AddSensor(board, "mb/fan/chassis", "Chassis Fan", SensorKind.Fan, 820f, 260f, 70f, 25f, 0f, 1800f);
        _hardware.Add(board);
    }

    private void BuildSmartDrives()
    {
        _drives.Add(new SmartDrive("disk0", "Samsung SSD 990 PRO 2TB")
        {
            Interface = "NVMe",
            Firmware = "4B2QJXD7",
            SerialNumber = "S6Z1NJ0W500123",
            CapacityBytes = 2_000_398_934_016,
            TemperatureCelsius = 44,
            PowerOnHours = 3_120,
            PowerCycleCount = 412,
            HostWritesTerabytes = 38.4,
        }
            .AddAttribute(new SmartAttribute(0x05, "Reallocated Sectors Count", 100, 100, 10, 0))
            .AddAttribute(new SmartAttribute(0x09, "Power-On Hours", 99, 99, 0, 3_120))
            .AddAttribute(new SmartAttribute(0x0C, "Power Cycle Count", 99, 99, 0, 412))
            .AddAttribute(new SmartAttribute(0xC2, "Temperature", 56, 48, 0, 44))
            .AddAttribute(new SmartAttribute(0xC5, "Current Pending Sector Count", 100, 100, 0, 0)));

        _drives.Add(new SmartDrive("disk1", "Seagate BarraCuda 4TB")
        {
            Interface = "SATA",
            Firmware = "CC43",
            SerialNumber = "ZFN0AB12",
            CapacityBytes = 4_000_787_030_016,
            TemperatureCelsius = 38,
            PowerOnHours = 14_890,
            PowerCycleCount = 1_204,
        }
            .AddAttribute(new SmartAttribute(0x05, "Reallocated Sectors Count", 100, 100, 36, 0))
            .AddAttribute(new SmartAttribute(0x09, "Power-On Hours", 74, 74, 0, 14_890))
            .AddAttribute(new SmartAttribute(0xC2, "Temperature", 38, 45, 0, 38))
            .AddAttribute(new SmartAttribute(0xC5, "Current Pending Sector Count", 100, 100, 0, 0)));
    }

    private SystemSummary BuildSummary() => new()
    {
        OperatingSystem = "Windows 11 Pro",
        OsVersion = "24H2 (Build 26100)",
        MachineName = "CHIPGAUGE-DEMO",
        Architecture = "x64",
        Manufacturer = "ASUS",
        Model = "System Product Name",
        Uptime = TimeSpan.FromHours(6.5),
    };

    private void AddSensor(
        HardwareItem parent,
        string id,
        string name,
        SensorKind kind,
        float baseline,
        float amplitude,
        float periodTicks,
        float noise,
        float min,
        float max,
        string? unit = null)
    {
        parent.AddSensor(new Sensor(id, name, kind, unit));
        int channel = _channel++;
        float phase = channel * 0.7f;
        _signals[id] = new Signal(_seed, channel, baseline, amplitude, periodTicks, phase, noise, min, max);
        _sensorIds.Add(id);
    }
}
