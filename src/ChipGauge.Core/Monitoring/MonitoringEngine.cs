using ChipGauge.Core.Abstractions;
using ChipGauge.Core.Models;

namespace ChipGauge.Core.Monitoring;

/// <summary>
/// The heart of ChipGauge. Owns the hardware tree discovered from an
/// <see cref="ISensorProvider"/>, polls it on a background cadence, folds each
/// reading into the corresponding <see cref="Sensor"/> statistics and
/// <see cref="HistoryStore"/>, and raises <see cref="Sampled"/> once per pass.
/// </summary>
/// <remarks>
/// <see cref="SampleOnce"/> performs one synchronous pass and is the unit-testable
/// core; <see cref="Start"/>/<see cref="StopAsync"/> simply drive it on a timer.
/// </remarks>
public sealed class MonitoringEngine : IAsyncDisposable
{
    private readonly ISensorProvider _provider;
    private readonly IClock _clock;
    private readonly MonitoringOptions _options;
    private readonly HistoryStore _history;
    private readonly IReadOnlyList<HardwareItem> _hardware;
    private readonly Dictionary<string, Sensor> _sensorsById;

    private CancellationTokenSource? _cts;
    private Task? _loop;
    private PeriodicTimer? _timer;
    private bool _disposed;

    /// <summary>Creates an engine over a provider.</summary>
    /// <param name="provider">The hardware/sensor source.</param>
    /// <param name="options">Sampling options; defaults are used when null.</param>
    /// <param name="clock">Clock abstraction; <see cref="SystemClock"/> is used when null.</param>
    public MonitoringEngine(ISensorProvider provider, MonitoringOptions? options = null, IClock? clock = null)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
        _options = options ?? new MonitoringOptions();
        _clock = clock ?? SystemClock.Instance;
        _history = new HistoryStore(_options.HistoryCapacity);

        _hardware = provider.GetHardware();
        _sensorsById = new Dictionary<string, Sensor>(StringComparer.Ordinal);
        foreach (HardwareItem item in _hardware)
        {
            foreach (Sensor sensor in item.EnumerateSensors())
            {
                _sensorsById[sensor.Id] = sensor;
            }
        }
    }

    /// <summary>Raised after each sampling pass with the batch of readings.</summary>
    public event EventHandler<MonitoringSample>? Sampled;

    /// <summary>The discovered hardware tree (stable for the engine's lifetime).</summary>
    public IReadOnlyList<HardwareItem> Hardware => _hardware;

    /// <summary>The rolling per-sensor history.</summary>
    public HistoryStore History => _history;

    /// <summary>The number of distinct sensors being tracked.</summary>
    public int SensorCount => _sensorsById.Count;

    /// <summary>
    /// The sampling interval (clamped by <see cref="MonitoringOptions"/>). Setting it
    /// takes effect immediately, even while the background loop is running.
    /// </summary>
    public TimeSpan Interval
    {
        get => _options.Interval;
        set
        {
            _options.Interval = value; // clamps
            if (_timer is not null)
            {
                _timer.Period = _options.Interval;
            }
        }
    }

    /// <summary>True while the background sampling loop is running.</summary>
    public bool IsRunning => _loop is { IsCompleted: false };

    /// <summary>Looks up a tracked sensor by id.</summary>
    public bool TryGetSensor(string sensorId, out Sensor? sensor) =>
        _sensorsById.TryGetValue(sensorId, out sensor);

    /// <summary>
    /// Performs one synchronous sampling pass: reads the provider, updates each
    /// sensor's value/statistics and history, then raises <see cref="Sampled"/>.
    /// </summary>
    /// <returns>The batch captured in this pass.</returns>
    public MonitoringSample SampleOnce()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        DateTimeOffset now = _clock.UtcNow;
        IReadOnlyList<SensorReading> readings = _provider.Sample();

        foreach (SensorReading reading in readings)
        {
            if (_sensorsById.TryGetValue(reading.SensorId, out Sensor? sensor))
            {
                sensor.Update(reading.Value);
                _history.Record(reading.SensorId, now, reading.Value);
            }
        }

        var sample = new MonitoringSample(now, readings);
        Sampled?.Invoke(this, sample);
        return sample;
    }

    /// <summary>Resets the running min/max/average statistics on every sensor.</summary>
    public void ResetStatistics()
    {
        foreach (Sensor sensor in _sensorsById.Values)
        {
            sensor.ResetStatistics();
        }
    }

    /// <summary>Starts the background sampling loop. Idempotent while already running.</summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsRunning)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _loop = RunAsync(_cts.Token);
    }

    /// <summary>Stops the background sampling loop and waits for it to finish.</summary>
    public async Task StopAsync()
    {
        if (_cts is null || _loop is null)
        {
            return;
        }

        await _cts.CancelAsync().ConfigureAwait(false);
        try
        {
            await _loop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation.
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _loop = null;
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        // Emit an immediate first sample so the UI is populated without waiting a full tick.
        SampleOnce();

        var timer = new PeriodicTimer(_options.Interval);
        _timer = timer;
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                SampleOnce();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation.
        }
        finally
        {
            _timer = null;
            timer.Dispose();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync().ConfigureAwait(false);
        _provider.Dispose();
    }
}
