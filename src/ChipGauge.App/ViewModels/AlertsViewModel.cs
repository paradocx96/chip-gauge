using System.Collections.ObjectModel;
using System.Globalization;
using ChipGauge.App.Services;
using ChipGauge.Core.Alerts;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// The Alerts page: a small editor for threshold rules plus the live list of
/// configured rules and which are currently firing. Owns an <see cref="AlertEngine"/>
/// fed one <see cref="MonitoringSample"/> per tick; rule changes are persisted through
/// an <see cref="AlertRulesStore"/>. The firing count surfaces as a navigation badge.
/// </summary>
public sealed partial class AlertsViewModel : PageViewModel
{
    private readonly MonitoringEngine _engine;
    private readonly AlertEngine _alerts = new();
    private readonly AlertRulesStore _store;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))]
    private AlertSensorOption? _selectedSensor;

    [ObservableProperty]
    private ComparisonOption _selectedComparison;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddRuleCommand))]
    private string _thresholdText = string.Empty;

    [ObservableProperty]
    private double _durationSeconds;

    [ObservableProperty]
    private string _message = string.Empty;

    public AlertsViewModel(Localizer localizer, MonitoringEngine engine, AlertRulesStore store)
        : base(localizer, "Nav_Alerts", "alerts")
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(store);
        _engine = engine;
        _store = store;

        Sensors = BuildSensorOptions(engine);
        _selectedSensor = Sensors.Count > 0 ? Sensors[0] : null;

        Comparisons =
        [
            new ComparisonOption(AlertComparison.Above, localizer, "Alerts_Above"),
            new ComparisonOption(AlertComparison.Below, localizer, "Alerts_Below"),
        ];
        _selectedComparison = Comparisons[0];
        localizer.PropertyChanged += OnLocalizerChanged;

        _alerts.Triggered += OnAlertTriggered;
        _alerts.Cleared += OnAlertCleared;

        foreach (AlertRule rule in store.Load())
        {
            _alerts.AddRule(rule);
            Rules.Add(new AlertRuleViewModel(rule, engine, localizer));
        }

        UpdateBadge();
    }

    /// <summary>Every sensor that can be watched, labelled by owning device.</summary>
    public IReadOnlyList<AlertSensorOption> Sensors { get; }

    /// <summary>The available breach directions (localized labels).</summary>
    public IReadOnlyList<ComparisonOption> Comparisons { get; }

    /// <summary>The configured rules.</summary>
    public ObservableCollection<AlertRuleViewModel> Rules { get; } = [];

    /// <summary>True when at least one rule is configured.</summary>
    public bool HasRules => Rules.Count > 0;

    /// <summary>The unit of the sensor selected in the editor (shown by the threshold input).</summary>
    public string SelectedSensorUnit => SelectedSensor?.Unit ?? string.Empty;

    /// <summary>Evaluates all rules against a monitoring sample (called on the UI thread).</summary>
    public void Evaluate(MonitoringSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        _alerts.Evaluate(sample.Readings, sample.Timestamp);
    }

    /// <inheritdoc />
    public override void OnTick()
    {
        foreach (AlertRuleViewModel rule in Rules)
        {
            rule.Refresh();
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddRule))]
    private void AddRule()
    {
        if (SelectedSensor is null || !TryParseThreshold(out float threshold))
        {
            return;
        }

        var rule = new AlertRule(
            Guid.NewGuid().ToString("N"),
            SelectedSensor.Id,
            SelectedComparison.Value,
            threshold,
            TimeSpan.FromSeconds(Math.Max(0, DurationSeconds)),
            string.IsNullOrWhiteSpace(Message) ? null : Message.Trim());

        _alerts.AddRule(rule);
        Rules.Add(new AlertRuleViewModel(rule, _engine, Localizer));
        OnPropertyChanged(nameof(HasRules));
        Persist();

        ThresholdText = string.Empty;
        Message = string.Empty;
    }

    private bool CanAddRule() => SelectedSensor is not null && TryParseThreshold(out _);

    [RelayCommand]
    private void RemoveRule(AlertRuleViewModel? rule)
    {
        if (rule is null)
        {
            return;
        }

        _alerts.RemoveRule(rule.Id);
        Rules.Remove(rule);
        OnPropertyChanged(nameof(HasRules));
        UpdateBadge();
        Persist();
    }

    private bool TryParseThreshold(out float value) =>
        float.TryParse(ThresholdText, NumberStyles.Float, CultureInfo.CurrentCulture, out value);

    private void OnAlertTriggered(object? sender, AlertEvent e)
    {
        SetFiring(e.Rule.Id, firing: true);
        UpdateBadge();
    }

    private void OnAlertCleared(object? sender, AlertRule rule)
    {
        SetFiring(rule.Id, firing: false);
        UpdateBadge();
    }

    private void SetFiring(string ruleId, bool firing)
    {
        foreach (AlertRuleViewModel vm in Rules)
        {
            if (string.Equals(vm.Id, ruleId, StringComparison.Ordinal))
            {
                vm.IsFiring = firing;
                break;
            }
        }
    }

    private void UpdateBadge() => BadgeCount = _alerts.ActiveRuleIds.Count;

    private void Persist() => _store.Save(Rules.Select(r => r.Rule));

    partial void OnSelectedSensorChanged(AlertSensorOption? value) => OnPropertyChanged(nameof(SelectedSensorUnit));

    private void OnLocalizerChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Refresh the comparison labels in place so the editor picker tracks a live
        // language change (the selection is preserved because the list is not rebuilt).
        if (e.PropertyName is "Item[]" or null)
        {
            foreach (ComparisonOption option in Comparisons)
            {
                option.RefreshLabel();
            }
        }
    }

    private static List<AlertSensorOption> BuildSensorOptions(MonitoringEngine engine)
    {
        var options = new List<AlertSensorOption>();
        foreach (HardwareItem device in engine.Hardware)
        {
            foreach (Sensor sensor in device.EnumerateSensors())
            {
                options.Add(new AlertSensorOption(sensor.Id, $"{device.Name} — {sensor.Name}", sensor.Unit));
            }
        }

        return options;
    }
}

/// <summary>
/// A breach direction paired with its localized label for the editor picker. The
/// label is resolved live from the <see cref="Localizer"/> so switching language
/// updates the open dropdown without rebuilding (and losing) the selection.
/// </summary>
public sealed class ComparisonOption : ObservableObject
{
    private readonly Localizer _localizer;
    private readonly string _key;

    public ComparisonOption(AlertComparison value, Localizer localizer, string key)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        Value = value;
        _localizer = localizer;
        _key = key;
    }

    /// <summary>The comparison this option represents.</summary>
    public AlertComparison Value { get; }

    /// <summary>The localized display label (e.g. "Above").</summary>
    public string Label => _localizer[_key];

    /// <summary>Raises a change notification so bound labels re-read after a language switch.</summary>
    public void RefreshLabel() => OnPropertyChanged(nameof(Label));
}
