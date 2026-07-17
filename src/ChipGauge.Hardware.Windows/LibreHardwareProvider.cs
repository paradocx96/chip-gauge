using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ChipGauge.Core.Abstractions;
using ChipGauge.Core.Models;
using ChipGauge.Core.Storage;
using LibreHardwareMonitor.Hardware;

namespace ChipGauge.Hardware.Windows;

/// <summary>
/// Live hardware provider backed by LibreHardwareMonitor. It discovers the machine
/// once, then refreshes and reads every sensor on each <see cref="Sample"/>.
/// </summary>
/// <remarks>
/// Full sensor coverage (MSR, SMBus, embedded controller, SMART) requires the
/// signed kernel driver, which needs elevation. Without it, LibreHardwareMonitor
/// returns the subset it can read — the app degrades gracefully rather than failing.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class LibreHardwareProvider : ISensorProvider, IHardwareInfoProvider
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _visitor = new();
    private readonly List<HardwareItem> _hardware = [];
    private readonly List<string> _sensorOrder = [];
    private readonly Dictionary<string, ISensor> _sensorsById = new(StringComparer.Ordinal);

    /// <summary>Opens LibreHardwareMonitor and builds the hardware tree.</summary>
    public LibreHardwareProvider()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true,
            IsNetworkEnabled = true,
            IsControllerEnabled = true,
            IsBatteryEnabled = true,
            IsPsuEnabled = true,
        };

        try
        {
            _computer.Open();
            _computer.Accept(_visitor);
            foreach (IHardware hardware in _computer.Hardware)
            {
                _hardware.Add(BuildItem(hardware));
            }

            IsAvailable = _hardware.Count > 0;
        }
        catch (Exception ex) when (ex is DllNotFoundException or InvalidOperationException or UnauthorizedAccessException)
        {
            IsAvailable = false;
        }
    }

    /// <inheritdoc />
    public string Name => "LibreHardwareMonitor";

    /// <inheritdoc />
    public bool IsAvailable { get; }

    /// <inheritdoc />
    public bool RequiresElevation => true;

    /// <inheritdoc />
    public bool IsSimulated => false;

    /// <inheritdoc />
    public IReadOnlyList<HardwareItem> GetHardware() => _hardware;

    /// <inheritdoc />
    public IReadOnlyList<SensorReading> Sample()
    {
        _computer.Accept(_visitor);

        var readings = new List<SensorReading>(_sensorOrder.Count);
        foreach (string id in _sensorOrder)
        {
            if (_sensorsById.TryGetValue(id, out ISensor? sensor) && sensor.Value is { } value && !float.IsNaN(value))
            {
                readings.Add(new SensorReading(id, value));
            }
        }

        return readings;
    }

    /// <inheritdoc />
    public SystemSummary GetSystemSummary()
    {
        string? boardModel = _hardware
            .FirstOrDefault(h => h.Kind == HardwareKind.Motherboard)?.Name;

        return new SystemSummary
        {
            OperatingSystem = RuntimeInformation.OSDescription,
            OsVersion = Environment.OSVersion.Version.ToString(),
            MachineName = Environment.MachineName,
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            Model = boardModel,
            Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<SmartDrive> GetSmartDrives()
    {
        var drives = new List<SmartDrive>();
        foreach (IHardware hardware in _computer.Hardware.Where(h => h.HardwareType == HardwareType.Storage))
        {
            double? temperature = hardware.Sensors
                .FirstOrDefault(s => s.SensorType == SensorType.Temperature)?.Value;

            drives.Add(new SmartDrive(hardware.Identifier.ToString(), hardware.Name)
            {
                TemperatureCelsius = temperature,
            });
        }

        return drives;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            _computer.Close();
        }
        catch (Exception ex) when (ex is InvalidOperationException or DllNotFoundException)
        {
            // Nothing to release if the driver never loaded.
        }

        GC.SuppressFinalize(this);
    }

    private HardwareItem BuildItem(IHardware hardware)
    {
        var item = new HardwareItem(
            hardware.Identifier.ToString(),
            string.IsNullOrWhiteSpace(hardware.Name) ? hardware.HardwareType.ToString() : hardware.Name,
            HardwareTypeMapper.ToKind(hardware.HardwareType));

        foreach (ISensor sensor in hardware.Sensors.OrderBy(s => s.SensorType).ThenBy(s => s.Index))
        {
            SensorMapping mapping = SensorTypeMapper.Map(sensor.SensorType);
            if (mapping.Kind == SensorKind.Unknown)
            {
                continue;
            }

            string id = sensor.Identifier.ToString();
            if (!_sensorsById.ContainsKey(id))
            {
                item.AddSensor(new Sensor(id, SensorName(sensor), mapping.Kind, mapping.Unit));
                _sensorsById[id] = sensor;
                _sensorOrder.Add(id);
            }
        }

        foreach (IHardware sub in hardware.SubHardware)
        {
            item.AddSubHardware(BuildItem(sub));
        }

        return item;
    }

    private static string SensorName(ISensor sensor) =>
        string.IsNullOrWhiteSpace(sensor.Name) ? sensor.SensorType.ToString() : sensor.Name;
}
