using System.Globalization;
using System.Text;
using ChipGauge.Core.Models;
using ChipGauge.Core.Storage;

namespace ChipGauge.Core.Reporting;

/// <summary>
/// Renders a point-in-time, human-readable report of the whole machine — the
/// "save a snapshot of my system" capability familiar from Speccy. Output is
/// deterministic and culture-invariant so reports diff cleanly and read the same
/// everywhere.
/// </summary>
public sealed class SnapshotService
{
    private const string Indent = "    ";

    /// <summary>
    /// Builds a plain-text report covering the system summary, the hardware tree
    /// with each sensor's current/min/max/avg, and per-drive SMART health.
    /// </summary>
    /// <param name="hardware">The hardware tree to report.</param>
    /// <param name="summary">Optional system summary header.</param>
    /// <param name="drives">Optional SMART drives section.</param>
    /// <param name="timestamp">Report timestamp; defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    public string CreateTextReport(
        IEnumerable<HardwareItem> hardware,
        SystemSummary? summary = null,
        IEnumerable<SmartDrive>? drives = null,
        DateTimeOffset? timestamp = null)
    {
        ArgumentNullException.ThrowIfNull(hardware);

        var sb = new StringBuilder();
        DateTimeOffset stamp = timestamp ?? DateTimeOffset.UtcNow;

        sb.AppendLine("ChipGauge System Snapshot");
        sb.Append("Generated: ")
          .AppendLine(stamp.ToString("u", CultureInfo.InvariantCulture));
        sb.AppendLine(new string('=', 60));
        sb.AppendLine();

        if (summary is not null)
        {
            AppendSummary(sb, summary);
        }

        foreach (HardwareItem item in hardware)
        {
            AppendHardware(sb, item, depth: 0);
        }

        if (drives is not null)
        {
            AppendDrives(sb, drives);
        }

        return sb.ToString();
    }

    private static void AppendSummary(StringBuilder sb, SystemSummary summary)
    {
        sb.AppendLine("[System]");
        AppendField(sb, "Operating System", summary.OperatingSystem);
        AppendField(sb, "OS Version", summary.OsVersion);
        AppendField(sb, "Machine Name", summary.MachineName);
        AppendField(sb, "Architecture", summary.Architecture);
        AppendField(sb, "Manufacturer", summary.Manufacturer);
        AppendField(sb, "Model", summary.Model);
        if (summary.Uptime is { } uptime)
        {
            AppendField(sb, "Uptime", FormatUptime(uptime));
        }

        sb.AppendLine();
    }

    private static void AppendHardware(StringBuilder sb, HardwareItem item, int depth)
    {
        var pad = string.Concat(Enumerable.Repeat(Indent, depth));

        if (depth == 0)
        {
            sb.Append('[').Append(item.Kind).Append("] ").AppendLine(item.Name);
        }
        else
        {
            sb.Append(pad).Append("- ").AppendLine(item.Name);
        }

        foreach (KeyValuePair<string, string> property in item.Properties)
        {
            sb.Append(pad).Append(Indent)
              .Append(property.Key).Append(": ").AppendLine(property.Value);
        }

        foreach (Sensor sensor in item.Sensors)
        {
            sb.Append(pad).Append(Indent)
              .Append(sensor.Name).Append(" = ").Append(sensor.FormatValue());

            if (sensor.Min is { } min && sensor.Max is { } max && sensor.Average is { } avg)
            {
                sb.Append(string.Create(
                    CultureInfo.InvariantCulture,
                    $"  (min {min:0.###}, max {max:0.###}, avg {avg:0.###})"));
            }

            sb.AppendLine();
        }

        foreach (HardwareItem child in item.SubHardware)
        {
            AppendHardware(sb, child, depth + 1);
        }

        if (depth == 0)
        {
            sb.AppendLine();
        }
    }

    private static void AppendDrives(StringBuilder sb, IEnumerable<SmartDrive> drives)
    {
        var any = false;
        foreach (SmartDrive drive in drives)
        {
            if (!any)
            {
                sb.AppendLine("[Storage Health (S.M.A.R.T.)]");
                any = true;
            }

            sb.Append(Indent).Append(drive.Model)
              .Append(" — Health: ").AppendLine(drive.Health.ToString());
            AppendField(sb, "Interface", drive.Interface, Indent + Indent);
            if (drive.TemperatureCelsius is { } temp)
            {
                AppendField(sb, "Temperature", string.Create(CultureInfo.InvariantCulture, $"{temp:0.#} °C"), Indent + Indent);
            }

            if (drive.PowerOnHours is { } hours)
            {
                AppendField(sb, "Power-On Hours", hours.ToString(CultureInfo.InvariantCulture), Indent + Indent);
            }

            if (drive.HostWritesTerabytes is { } tbw)
            {
                AppendField(sb, "Host Writes", string.Create(CultureInfo.InvariantCulture, $"{tbw:0.##} TB"), Indent + Indent);
            }

            foreach (SmartAttribute attribute in drive.Attributes)
            {
                sb.Append(Indent).Append(Indent)
                  .Append(string.Create(
                      CultureInfo.InvariantCulture,
                      $"{attribute.Id:X2} {attribute.Name}: cur {attribute.Current}, worst {attribute.Worst}, thr {attribute.Threshold}, raw {attribute.RawValue}"))
                  .AppendLine();
            }

            sb.AppendLine();
        }
    }

    private static void AppendField(StringBuilder sb, string label, string? value, string pad = Indent)
    {
        if (!string.IsNullOrEmpty(value))
        {
            sb.Append(pad).Append(label).Append(": ").AppendLine(value);
        }
    }

    private static string FormatUptime(TimeSpan uptime) => string.Create(
        CultureInfo.InvariantCulture,
        $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m");
}
