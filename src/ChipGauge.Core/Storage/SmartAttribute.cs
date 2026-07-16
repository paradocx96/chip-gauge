namespace ChipGauge.Core.Storage;

/// <summary>
/// A single S.M.A.R.T. attribute as reported by a drive: the normalized
/// current/worst/threshold triple plus the vendor-specific raw value.
/// </summary>
public sealed class SmartAttribute
{
    /// <summary>Creates a SMART attribute record.</summary>
    /// <param name="id">Attribute ID (e.g. 0x05 Reallocated Sectors Count).</param>
    /// <param name="name">Human-readable attribute name.</param>
    /// <param name="current">Normalized current value (typically 1–253).</param>
    /// <param name="worst">Normalized worst value ever recorded.</param>
    /// <param name="threshold">Normalized failure threshold; 0 means "no threshold".</param>
    /// <param name="rawValue">Vendor-specific raw value.</param>
    public SmartAttribute(byte id, string name, byte current, byte worst, byte threshold, long rawValue)
    {
        ArgumentNullException.ThrowIfNull(name);
        Id = id;
        Name = name;
        Current = current;
        Worst = worst;
        Threshold = threshold;
        RawValue = rawValue;
    }

    /// <summary>Attribute ID.</summary>
    public byte Id { get; }

    /// <summary>Human-readable attribute name.</summary>
    public string Name { get; }

    /// <summary>Normalized current value.</summary>
    public byte Current { get; }

    /// <summary>Normalized worst-ever value.</summary>
    public byte Worst { get; }

    /// <summary>Normalized failure threshold (0 = not applicable).</summary>
    public byte Threshold { get; }

    /// <summary>Vendor-specific raw value.</summary>
    public long RawValue { get; }

    /// <summary>
    /// True when a meaningful threshold exists and the current normalized value
    /// has reached or fallen below it — i.e. this attribute is failing.
    /// </summary>
    public bool IsBelowThreshold => Threshold > 0 && Current <= Threshold;
}
