using ChipGauge.Core.Models;

namespace ChipGauge.Core.Abstractions;

/// <summary>
/// The platform seam of ChipGauge. A provider discovers the hardware tree and,
/// on demand, samples current sensor values. The Windows implementation wraps
/// LibreHardwareMonitor; the Mock implementation simulates hardware; a future
/// macOS implementation will read SMC/IOKit — the engine and UI depend only on
/// this interface.
/// </summary>
public interface ISensorProvider : IDisposable
{
    /// <summary>A short name identifying the provider (for diagnostics/about screens).</summary>
    string Name { get; }

    /// <summary>
    /// True when the provider initialized successfully and can return live data.
    /// A provider may be present but unavailable (e.g. required elevation refused).
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// True when full functionality requires elevated privileges. The app uses
    /// this to decide whether to warn the user about a degraded, non-elevated mode.
    /// </summary>
    bool RequiresElevation { get; }

    /// <summary>
    /// True when the readings are simulated rather than read from real hardware.
    /// The UI surfaces a "demo data" indicator when this is set.
    /// </summary>
    bool IsSimulated { get; }

    /// <summary>
    /// True when the provider has the privileges it needs for full data. Always true
    /// when <see cref="RequiresElevation"/> is false; on Windows it reflects whether
    /// the process is elevated (the kernel driver for temperatures, power, fan speeds
    /// and SMART requires administrator rights).
    /// </summary>
    bool IsElevated { get; }

    /// <summary>
    /// True when low-level, driver-backed sensors (CPU/GPU/board temperatures and
    /// power) are actually being read. When the process is elevated but this is
    /// false, the kernel driver is present but blocked — commonly by Windows
    /// Memory Integrity / Core Isolation.
    /// </summary>
    bool HasLowLevelSensors { get; }

    /// <summary>
    /// Returns the discovered hardware tree. The returned <see cref="HardwareItem"/>
    /// instances are stable for the lifetime of the provider; <see cref="Sample"/>
    /// updates values by sensor id rather than replacing the tree.
    /// </summary>
    IReadOnlyList<HardwareItem> GetHardware();

    /// <summary>
    /// Polls the hardware and returns the current value of every sensor. Called on
    /// a background cadence by the monitoring engine; must be cheap and non-blocking.
    /// </summary>
    IReadOnlyList<SensorReading> Sample();
}
