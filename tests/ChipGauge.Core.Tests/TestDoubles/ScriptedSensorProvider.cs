using ChipGauge.Core.Abstractions;
using ChipGauge.Core.Models;

namespace ChipGauge.Core.Tests.TestDoubles;

/// <summary>
/// A minimal <see cref="ISensorProvider"/> whose readings are supplied by a script,
/// so a test can assert exactly how the engine folds specific values.
/// </summary>
internal sealed class ScriptedSensorProvider : ISensorProvider
{
    private readonly IReadOnlyList<HardwareItem> _hardware;
    private readonly Func<int, IReadOnlyList<SensorReading>> _script;
    private int _sampleIndex;

    public ScriptedSensorProvider(
        IReadOnlyList<HardwareItem> hardware,
        Func<int, IReadOnlyList<SensorReading>> script)
    {
        _hardware = hardware;
        _script = script;
    }

    public string Name => "Scripted";

    public bool IsAvailable => true;

    public bool RequiresElevation => false;

    public int SampleCallCount => _sampleIndex;

    public bool Disposed { get; private set; }

    public IReadOnlyList<HardwareItem> GetHardware() => _hardware;

    public IReadOnlyList<SensorReading> Sample() => _script(_sampleIndex++);

    public void Dispose() => Disposed = true;

    /// <summary>Builds a single-sensor tree for convenience.</summary>
    public static (HardwareItem Item, Sensor Sensor) SingleSensor(
        string sensorId = "s1",
        SensorKind kind = SensorKind.Temperature)
    {
        var sensor = new Sensor(sensorId, sensorId, kind);
        var item = new HardwareItem("hw", "Device", HardwareKind.Cpu).AddSensor(sensor);
        return (item, sensor);
    }
}
