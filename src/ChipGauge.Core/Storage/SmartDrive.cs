using System.Collections.Frozen;

namespace ChipGauge.Core.Storage;

/// <summary>
/// A storage device and its S.M.A.R.T. snapshot, with a computed
/// <see cref="Health"/> verdict comparable to CrystalDiskInfo's status light.
/// </summary>
public sealed class SmartDrive
{
    /// <summary>
    /// SMART attribute IDs whose non-zero raw value indicates developing wear and
    /// warrants a <see cref="DriveHealth.Caution"/> verdict even before any
    /// normalized value crosses its threshold.
    /// </summary>
    private static readonly FrozenSet<byte> CriticalRawAttributes = new byte[]
    {
        0x05, // Reallocated Sectors Count
        0xC4, // Reallocation Event Count
        0xC5, // Current Pending Sector Count
        0xC6, // Uncorrectable Sector Count (offline)
    }.ToFrozenSet();

    private readonly List<SmartAttribute> _attributes = [];

    /// <summary>Creates a drive record.</summary>
    /// <param name="id">Stable identifier (e.g. physical drive path).</param>
    /// <param name="model">Drive model string.</param>
    public SmartDrive(string id, string model)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(model);
        Id = id;
        Model = model;
    }

    /// <summary>Stable identifier.</summary>
    public string Id { get; }

    /// <summary>Drive model string.</summary>
    public string Model { get; }

    /// <summary>Firmware revision, if known.</summary>
    public string? Firmware { get; init; }

    /// <summary>Serial number, if known.</summary>
    public string? SerialNumber { get; init; }

    /// <summary>Bus interface (e.g. "NVMe", "SATA").</summary>
    public string? Interface { get; init; }

    /// <summary>Total capacity in bytes, if known.</summary>
    public long? CapacityBytes { get; init; }

    /// <summary>Current drive temperature in °C, if reported.</summary>
    public double? TemperatureCelsius { get; init; }

    /// <summary>Cumulative power-on time in hours, if reported.</summary>
    public long? PowerOnHours { get; init; }

    /// <summary>Power-cycle count, if reported.</summary>
    public long? PowerCycleCount { get; init; }

    /// <summary>Total data written to the host in terabytes (TBW), if reported.</summary>
    public double? HostWritesTerabytes { get; init; }

    /// <summary>The drive's SMART attributes.</summary>
    public IReadOnlyList<SmartAttribute> Attributes => _attributes;

    /// <summary>Adds a SMART attribute and returns this drive for fluent construction.</summary>
    public SmartDrive AddAttribute(SmartAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        _attributes.Add(attribute);
        return this;
    }

    /// <summary>
    /// The overall health verdict:
    /// <list type="bullet">
    /// <item><see cref="DriveHealth.Unknown"/> when no attributes are present;</item>
    /// <item><see cref="DriveHealth.Bad"/> when any attribute is at or below its failure threshold;</item>
    /// <item><see cref="DriveHealth.Caution"/> when a critical wear attribute has a non-zero raw value;</item>
    /// <item><see cref="DriveHealth.Good"/> otherwise.</item>
    /// </list>
    /// </summary>
    public DriveHealth Health
    {
        get
        {
            if (_attributes.Count == 0)
            {
                return DriveHealth.Unknown;
            }

            var caution = false;

            foreach (SmartAttribute attribute in _attributes)
            {
                if (attribute.IsBelowThreshold)
                {
                    return DriveHealth.Bad;
                }

                if (attribute.RawValue > 0 && CriticalRawAttributes.Contains(attribute.Id))
                {
                    caution = true;
                }
            }

            return caution ? DriveHealth.Caution : DriveHealth.Good;
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"{Model} [{Health}]";
}
