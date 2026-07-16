using ChipGauge.Core.Alerts;
using ChipGauge.Core.Models;

namespace ChipGauge.Core.Tests.Alerts;

public sealed class AlertEngineTests
{
    private static readonly DateTimeOffset Origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static IReadOnlyList<SensorReading> Reading(string id, float value) =>
        [new SensorReading(id, value)];

    [Fact]
    public void InstantRule_FiresOnFirstBreach()
    {
        var engine = new AlertEngine();
        engine.AddRule(new AlertRule("r1", "cpu/temp", AlertComparison.Above, 90f));
        var events = new List<AlertEvent>();
        engine.Triggered += (_, e) => events.Add(e);

        engine.Evaluate(Reading("cpu/temp", 95f), Origin);

        Assert.Single(events);
        Assert.Equal(95f, events[0].Value);
        Assert.Contains("r1", engine.ActiveRuleIds);
    }

    [Fact]
    public void Rule_DoesNotFireWhenWithinLimits()
    {
        var engine = new AlertEngine();
        engine.AddRule(new AlertRule("r1", "cpu/temp", AlertComparison.Above, 90f));
        var fired = false;
        engine.Triggered += (_, _) => fired = true;

        engine.Evaluate(Reading("cpu/temp", 70f), Origin);

        Assert.False(fired);
        Assert.Empty(engine.ActiveRuleIds);
    }

    [Fact]
    public void DurationRule_FiresOnlyAfterBreachHeldLongEnough()
    {
        var engine = new AlertEngine();
        engine.AddRule(new AlertRule("r1", "cpu/temp", AlertComparison.Above, 90f, TimeSpan.FromSeconds(10)));
        var events = new List<AlertEvent>();
        engine.Triggered += (_, e) => events.Add(e);

        engine.Evaluate(Reading("cpu/temp", 95f), Origin);                    // t=0  breach begins
        engine.Evaluate(Reading("cpu/temp", 96f), Origin.AddSeconds(5));      // still within grace period
        Assert.Empty(events);

        engine.Evaluate(Reading("cpu/temp", 97f), Origin.AddSeconds(10));     // duration reached -> fire
        Assert.Single(events);
    }

    [Fact]
    public void DurationRule_ResetsWhenBreachIsInterrupted()
    {
        var engine = new AlertEngine();
        engine.AddRule(new AlertRule("r1", "cpu/temp", AlertComparison.Above, 90f, TimeSpan.FromSeconds(10)));
        var events = new List<AlertEvent>();
        engine.Triggered += (_, e) => events.Add(e);

        engine.Evaluate(Reading("cpu/temp", 95f), Origin);                 // breach begins
        engine.Evaluate(Reading("cpu/temp", 80f), Origin.AddSeconds(5));   // interrupted -> timer resets
        engine.Evaluate(Reading("cpu/temp", 95f), Origin.AddSeconds(12));  // new breach begins
        engine.Evaluate(Reading("cpu/temp", 95f), Origin.AddSeconds(20));  // only 8s -> still no fire

        Assert.Empty(events);
    }

    [Fact]
    public void Rule_FiresAtMostOncePerBreachEpisode()
    {
        var engine = new AlertEngine();
        engine.AddRule(new AlertRule("r1", "cpu/temp", AlertComparison.Above, 90f));
        var count = 0;
        engine.Triggered += (_, _) => count++;

        engine.Evaluate(Reading("cpu/temp", 95f), Origin);
        engine.Evaluate(Reading("cpu/temp", 96f), Origin.AddSeconds(1));
        engine.Evaluate(Reading("cpu/temp", 97f), Origin.AddSeconds(2));

        Assert.Equal(1, count);
    }

    [Fact]
    public void Clear_RaisedWhenValueReturnsToNormal_AndRuleCanFireAgain()
    {
        var engine = new AlertEngine();
        engine.AddRule(new AlertRule("r1", "cpu/temp", AlertComparison.Above, 90f));
        var triggered = 0;
        var cleared = 0;
        engine.Triggered += (_, _) => triggered++;
        engine.Cleared += (_, _) => cleared++;

        engine.Evaluate(Reading("cpu/temp", 95f), Origin);                // fire
        engine.Evaluate(Reading("cpu/temp", 70f), Origin.AddSeconds(1));  // clear
        engine.Evaluate(Reading("cpu/temp", 95f), Origin.AddSeconds(2));  // fire again

        Assert.Equal(2, triggered);
        Assert.Equal(1, cleared);
    }

    [Fact]
    public void BelowComparison_FiresWhenValueDropsUnderThreshold()
    {
        var engine = new AlertEngine();
        engine.AddRule(new AlertRule("fan", "gpu/fan", AlertComparison.Below, 300f));
        var fired = false;
        engine.Triggered += (_, _) => fired = true;

        engine.Evaluate(Reading("gpu/fan", 150f), Origin);

        Assert.True(fired);
    }

    [Fact]
    public void Evaluate_IgnoresRulesWhoseSensorIsAbsent()
    {
        var engine = new AlertEngine();
        engine.AddRule(new AlertRule("r1", "missing", AlertComparison.Above, 1f));
        var fired = false;
        engine.Triggered += (_, _) => fired = true;

        engine.Evaluate(Reading("other", 100f), Origin);

        Assert.False(fired);
    }

    [Fact]
    public void RemoveRule_StopsEvaluationAndClearsState()
    {
        var engine = new AlertEngine();
        engine.AddRule(new AlertRule("r1", "cpu/temp", AlertComparison.Above, 90f));
        engine.Evaluate(Reading("cpu/temp", 95f), Origin);

        Assert.True(engine.RemoveRule("r1"));

        Assert.Empty(engine.Rules);
        Assert.Empty(engine.ActiveRuleIds);
    }

    [Fact]
    public void AddRule_WithDuplicateId_ReplacesRule()
    {
        var engine = new AlertEngine();
        engine.AddRule(new AlertRule("r1", "cpu/temp", AlertComparison.Above, 90f));
        engine.AddRule(new AlertRule("r1", "cpu/temp", AlertComparison.Above, 50f));

        Assert.Single(engine.Rules);
        var fired = false;
        engine.Triggered += (_, _) => fired = true;
        engine.Evaluate(Reading("cpu/temp", 60f), Origin); // above the replacement threshold of 50

        Assert.True(fired);
    }
}
