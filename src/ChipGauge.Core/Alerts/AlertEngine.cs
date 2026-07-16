using ChipGauge.Core.Models;

namespace ChipGauge.Core.Alerts;

/// <summary>
/// Evaluates <see cref="AlertRule"/>s against each monitoring sample and raises
/// edge-triggered notifications: <see cref="Triggered"/> the moment a rule's breach
/// has held for its required duration, and <see cref="Cleared"/> when the value
/// returns to normal. A rule fires at most once per breach episode.
/// </summary>
public sealed class AlertEngine
{
    private readonly Dictionary<string, AlertRule> _rules = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> _breachStartedAt = new(StringComparer.Ordinal);
    private readonly HashSet<string> _firing = new(StringComparer.Ordinal);

    /// <summary>Raised when a rule transitions into the firing state.</summary>
    public event EventHandler<AlertEvent>? Triggered;

    /// <summary>Raised when a firing rule's condition clears.</summary>
    public event EventHandler<AlertRule>? Cleared;

    /// <summary>The currently registered rules.</summary>
    public IReadOnlyCollection<AlertRule> Rules => _rules.Values;

    /// <summary>The ids of rules currently in the firing state.</summary>
    public IReadOnlyCollection<string> ActiveRuleIds => _firing;

    /// <summary>Registers or replaces a rule (keyed by <see cref="AlertRule.Id"/>).</summary>
    public void AddRule(AlertRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        _rules[rule.Id] = rule;
    }

    /// <summary>Removes a rule and clears any pending/firing state for it.</summary>
    public bool RemoveRule(string ruleId)
    {
        ArgumentException.ThrowIfNullOrEmpty(ruleId);
        _breachStartedAt.Remove(ruleId);
        _firing.Remove(ruleId);
        return _rules.Remove(ruleId);
    }

    /// <summary>
    /// Evaluates every rule against the readings captured at <paramref name="timestamp"/>,
    /// raising <see cref="Triggered"/> / <see cref="Cleared"/> as states change.
    /// </summary>
    public void Evaluate(IReadOnlyList<SensorReading> readings, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(readings);
        if (_rules.Count == 0)
        {
            return;
        }

        Dictionary<string, float> byId = BuildLookup(readings);

        foreach (AlertRule rule in _rules.Values)
        {
            if (!byId.TryGetValue(rule.SensorId, out float value))
            {
                continue;
            }

            if (rule.IsBreached(value))
            {
                EvaluateBreach(rule, value, timestamp);
            }
            else
            {
                ClearBreach(rule, timestamp);
            }
        }
    }

    private void EvaluateBreach(AlertRule rule, float value, DateTimeOffset timestamp)
    {
        if (!_breachStartedAt.TryGetValue(rule.Id, out DateTimeOffset startedAt))
        {
            startedAt = timestamp;
            _breachStartedAt[rule.Id] = startedAt;
        }

        if (!_firing.Contains(rule.Id) && timestamp - startedAt >= rule.Duration)
        {
            _firing.Add(rule.Id);
            Triggered?.Invoke(this, new AlertEvent(rule, timestamp, value));
        }
    }

    private void ClearBreach(AlertRule rule, DateTimeOffset timestamp)
    {
        _ = timestamp;
        _breachStartedAt.Remove(rule.Id);
        if (_firing.Remove(rule.Id))
        {
            Cleared?.Invoke(this, rule);
        }
    }

    private static Dictionary<string, float> BuildLookup(IReadOnlyList<SensorReading> readings)
    {
        var byId = new Dictionary<string, float>(readings.Count, StringComparer.Ordinal);
        foreach (SensorReading reading in readings)
        {
            byId[reading.SensorId] = reading.Value;
        }

        return byId;
    }
}
