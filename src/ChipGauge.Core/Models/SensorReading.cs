namespace ChipGauge.Core.Models;

/// <summary>
/// A single value produced by a sensor provider during one sampling pass.
/// Immutable and allocation-cheap so a full sample can be produced every tick.
/// </summary>
/// <param name="SensorId">The stable identifier of the sensor this value belongs to.</param>
/// <param name="Value">The measured value in the sensor's unit.</param>
public readonly record struct SensorReading(string SensorId, float Value);
