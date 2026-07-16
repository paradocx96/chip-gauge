namespace ChipGauge.Core.Storage;

/// <summary>
/// Overall health verdict for a storage device, derived from its SMART data —
/// the traffic-light status CrystalDiskInfo surfaces.
/// </summary>
public enum DriveHealth
{
    /// <summary>Health could not be determined (no SMART data available).</summary>
    Unknown = 0,

    /// <summary>All attributes are within spec.</summary>
    Good,

    /// <summary>Early-warning indicators are non-zero; the drive should be watched.</summary>
    Caution,

    /// <summary>One or more attributes have crossed their failure threshold.</summary>
    Bad,
}
