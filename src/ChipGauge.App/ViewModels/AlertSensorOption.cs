namespace ChipGauge.App.ViewModels;

/// <summary>
/// One entry in the alert rule editor's sensor picker: a sensor a rule can watch,
/// labelled with its owning device so duplicate sensor names stay distinguishable.
/// </summary>
/// <param name="Id">The sensor id the rule stores.</param>
/// <param name="DisplayName">"<c>Device — Sensor</c>" label shown in the picker.</param>
/// <param name="Unit">The sensor's unit symbol (shown next to the threshold input).</param>
public sealed record AlertSensorOption(string Id, string DisplayName, string Unit);
