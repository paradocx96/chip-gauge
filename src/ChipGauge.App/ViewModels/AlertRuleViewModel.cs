using System.Globalization;
using ChipGauge.App.Services;
using ChipGauge.Core.Alerts;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// Presents one configured <see cref="AlertRule"/>: the sensor it watches, its
/// condition in words, the sensor's live value, and whether the rule is currently
/// firing. The firing flag is driven by the <see cref="AlertEngine"/>'s edge events;
/// the live value is pulled on each tick.
/// </summary>
public sealed partial class AlertRuleViewModel : ObservableObject
{
    private readonly SensorViewModel? _sensor;

    [ObservableProperty]
    private bool _isFiring;

    public AlertRuleViewModel(AlertRule rule, MonitoringEngine engine, Localizer localizer)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(localizer);

        Rule = rule;
        if (engine.TryGetSensor(rule.SensorId, out Sensor? sensor) && sensor is not null)
        {
            _sensor = new SensorViewModel(sensor, engine.History.GetOrCreate(sensor.Id));
            SensorName = sensor.Name;
            Unit = sensor.Unit;
        }
        else
        {
            SensorName = rule.SensorId;
            Unit = string.Empty;
        }

        string comparison = rule.Comparison == AlertComparison.Above ? "≥" : "≤";
        string threshold = string.Create(CultureInfo.CurrentCulture, $"{rule.Threshold:0.#} {Unit}").TrimEnd();
        string condition = $"{comparison} {threshold}";
        if (rule.Duration > TimeSpan.Zero)
        {
            condition += string.Create(CultureInfo.CurrentCulture, $" · {rule.Duration.TotalSeconds:0}s");
        }

        Condition = condition;
        _ = localizer; // reserved for future localized descriptors
    }

    /// <summary>The underlying rule.</summary>
    public AlertRule Rule { get; }

    /// <summary>The rule's stable id (used by remove/lookup).</summary>
    public string Id => Rule.Id;

    /// <summary>The watched sensor's display name.</summary>
    public string SensorName { get; }

    /// <summary>The watched sensor's unit symbol.</summary>
    public string Unit { get; }

    /// <summary>The condition in words, e.g. "≥ 90 °C · 10s".</summary>
    public string Condition { get; }

    /// <summary>The optional user message, or the condition when none was given.</summary>
    public string Message => string.IsNullOrWhiteSpace(Rule.Message) ? Condition : Rule.Message;

    /// <summary>The watched sensor's current formatted value (or "—" when unavailable).</summary>
    public string CurrentValue => _sensor?.Value ?? "—";

    /// <summary>Re-reads the sensor's live value.</summary>
    public void Refresh()
    {
        _sensor?.Refresh();
        OnPropertyChanged(nameof(CurrentValue));
    }
}
