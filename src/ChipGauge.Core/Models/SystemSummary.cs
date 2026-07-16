namespace ChipGauge.Core.Models;

/// <summary>
/// High-level description of the host machine shown on the dashboard summary card
/// and at the top of an exported snapshot report.
/// </summary>
public sealed record SystemSummary
{
    /// <summary>Operating system product name (e.g. "Windows 11 Pro").</summary>
    public required string OperatingSystem { get; init; }

    /// <summary>OS version / build string.</summary>
    public string? OsVersion { get; init; }

    /// <summary>Machine / host name.</summary>
    public string? MachineName { get; init; }

    /// <summary>Process/OS architecture (e.g. "x64", "ARM64").</summary>
    public string? Architecture { get; init; }

    /// <summary>System manufacturer (e.g. "ASUS").</summary>
    public string? Manufacturer { get; init; }

    /// <summary>System model / product name.</summary>
    public string? Model { get; init; }

    /// <summary>Time since the machine last booted.</summary>
    public TimeSpan? Uptime { get; init; }
}
