namespace ChipGauge.Core.Models;

/// <summary>
/// The category of a <see cref="HardwareItem"/>. Maps one-to-one onto the
/// navigation categories in the UI (Dashboard aggregates all of them).
/// </summary>
public enum HardwareKind
{
    /// <summary>Unrecognized device.</summary>
    Unknown = 0,

    /// <summary>Central processing unit (package, cores, caches).</summary>
    Cpu,

    /// <summary>Graphics processor (discrete or integrated).</summary>
    Gpu,

    /// <summary>System memory (RAM) and its modules.</summary>
    Memory,

    /// <summary>A storage device (SSD/NVMe/HDD).</summary>
    Storage,

    /// <summary>A network adapter.</summary>
    Network,

    /// <summary>Mainboard / motherboard, chipset and firmware.</summary>
    Motherboard,

    /// <summary>Battery / power source.</summary>
    Battery,

    /// <summary>A cooling device (fan controller, AIO pump).</summary>
    Cooler,
}
