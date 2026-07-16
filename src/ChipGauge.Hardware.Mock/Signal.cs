namespace ChipGauge.Hardware.Mock;

/// <summary>
/// A deterministic waveform generator for one simulated sensor. Produces a
/// smooth baseline plus a sine oscillation and reproducible pseudo-noise, so the
/// same seed and tick always yield the same value — reproducible demos and tests.
/// </summary>
internal sealed class Signal
{
    private readonly float _baseline;
    private readonly float _amplitude;
    private readonly float _periodTicks;
    private readonly float _phase;
    private readonly float _noise;
    private readonly float _min;
    private readonly float _max;
    private readonly int _seed;
    private readonly int _channel;

    public Signal(
        int seed,
        int channel,
        float baseline,
        float amplitude,
        float periodTicks,
        float phase,
        float noise,
        float min,
        float max)
    {
        _seed = seed;
        _channel = channel;
        _baseline = baseline;
        _amplitude = amplitude;
        _periodTicks = periodTicks <= 0 ? 1f : periodTicks;
        _phase = phase;
        _noise = noise;
        _min = min;
        _max = max;
    }

    /// <summary>Evaluates the waveform at a discrete tick.</summary>
    public float ValueAt(long tick)
    {
        double angle = (2.0 * Math.PI * tick / _periodTicks) + _phase;
        double value = _baseline + (_amplitude * Math.Sin(angle));
        value += _noise * PseudoNoise(tick);
        return (float)Math.Clamp(value, _min, _max);
    }

    /// <summary>A reproducible value in [-1, 1] derived from the seed, channel and tick.</summary>
    private float PseudoNoise(long tick)
    {
        unchecked
        {
            uint h = (uint)_seed;
            h = (h * 2654435761u) ^ (uint)_channel;
            h = (h * 2246822519u) ^ (uint)tick;
            h = (h * 3266489917u) ^ (uint)(tick >> 32);
            h ^= h >> 15;
            h *= 2246822519u;
            h ^= h >> 13;
            // Map to [-1, 1].
            return ((h / (float)uint.MaxValue) * 2f) - 1f;
        }
    }
}
