using ChipGauge.Core.Models;
using ChipGauge.Core.Storage;

namespace ChipGauge.Core.Abstractions;

/// <summary>
/// Supplies relatively static system information that is refreshed occasionally
/// rather than every sampling tick: the machine summary and per-drive SMART data.
/// </summary>
public interface IHardwareInfoProvider
{
    /// <summary>Returns a high-level description of the host machine.</summary>
    SystemSummary GetSystemSummary();

    /// <summary>Returns the SMART snapshot for every storage device.</summary>
    IReadOnlyList<SmartDrive> GetSmartDrives();
}
