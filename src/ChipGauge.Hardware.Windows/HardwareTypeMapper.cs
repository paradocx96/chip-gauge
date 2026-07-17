using ChipGauge.Core.Models;
using LibreHardwareMonitor.Hardware;

namespace ChipGauge.Hardware.Windows;

/// <summary>Maps LibreHardwareMonitor <see cref="HardwareType"/> values onto ChipGauge categories.</summary>
public static class HardwareTypeMapper
{
    /// <summary>Returns the <see cref="HardwareKind"/> for a LibreHardwareMonitor hardware type.</summary>
    public static HardwareKind ToKind(HardwareType type) => type switch
    {
        HardwareType.Cpu => HardwareKind.Cpu,
        HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => HardwareKind.Gpu,
        HardwareType.Memory => HardwareKind.Memory,
        HardwareType.Storage => HardwareKind.Storage,
        HardwareType.Network => HardwareKind.Network,
        HardwareType.Motherboard or HardwareType.SuperIO or HardwareType.EmbeddedController or HardwareType.Psu
            => HardwareKind.Motherboard,
        HardwareType.Cooler => HardwareKind.Cooler,
        HardwareType.Battery => HardwareKind.Battery,
        _ => HardwareKind.Unknown,
    };
}
