using ChipGauge.App.Services;
using ChipGauge.App.ViewModels;
using ChipGauge.Core.Abstractions;
using ChipGauge.Core.Models;
using ChipGauge.Core.Monitoring;

namespace ChipGauge.App.Tests;

public sealed class AlertsViewModelTests
{
    private static string TempStorePath() =>
        Path.Combine(Path.GetTempPath(), "ChipGauge-Tests", Guid.NewGuid().ToString("N") + ".json");

    private static AlertsViewModel BuildAlerts(MonitoringEngine engine, AlertRulesStore? store = null)
    {
        var localizer = new Localizer();
        return new AlertsViewModel(localizer, engine, store ?? new AlertRulesStore(TempStorePath()));
    }

    [Fact]
    public async Task AddRule_AddsConfiguredRule()
    {
        var provider = new ControllableProvider();
        await using var engine = new MonitoringEngine(provider);
        AlertsViewModel vm = BuildAlerts(engine);

        Assert.False(vm.HasRules);

        vm.SelectedSensor = vm.Sensors[0];
        vm.ThresholdText = "90";
        vm.AddRuleCommand.Execute(null);

        Assert.True(vm.HasRules);
        Assert.Single(vm.Rules);
        Assert.Equal(string.Empty, vm.ThresholdText); // form resets after add
    }

    [Fact]
    public async Task Breach_FiresRule_AndDrivesNavBadge()
    {
        var provider = new ControllableProvider();
        await using var engine = new MonitoringEngine(provider);
        AlertsViewModel vm = BuildAlerts(engine);

        vm.SelectedSensor = vm.Sensors[0];
        vm.SelectedComparison = vm.Comparisons.First(c => c.Value == Core.Alerts.AlertComparison.Above);
        vm.ThresholdText = "90";
        vm.AddRuleCommand.Execute(null);

        // Below threshold -> not firing, no badge.
        provider.NextValue = 60f;
        vm.Evaluate(engine.SampleOnce());
        Assert.False(vm.Rules[0].IsFiring);
        Assert.Equal(0, vm.BadgeCount);
        Assert.False(vm.HasBadge);

        // Above threshold (duration 0) -> fires immediately, badge shows 1.
        provider.NextValue = 95f;
        vm.Evaluate(engine.SampleOnce());
        Assert.True(vm.Rules[0].IsFiring);
        Assert.Equal(1, vm.BadgeCount);
        Assert.True(vm.HasBadge);

        // Back to normal -> clears.
        provider.NextValue = 55f;
        vm.Evaluate(engine.SampleOnce());
        Assert.False(vm.Rules[0].IsFiring);
        Assert.Equal(0, vm.BadgeCount);
    }

    [Fact]
    public async Task RemoveRule_ClearsRuleAndBadge()
    {
        var provider = new ControllableProvider();
        await using var engine = new MonitoringEngine(provider);
        AlertsViewModel vm = BuildAlerts(engine);

        vm.SelectedSensor = vm.Sensors[0];
        vm.ThresholdText = "90";
        vm.AddRuleCommand.Execute(null);

        provider.NextValue = 99f;
        vm.Evaluate(engine.SampleOnce());
        Assert.Equal(1, vm.BadgeCount);

        vm.RemoveRuleCommand.Execute(vm.Rules[0]);

        Assert.False(vm.HasRules);
        Assert.Empty(vm.Rules);
        Assert.Equal(0, vm.BadgeCount);
    }

    [Fact]
    public async Task Rules_PersistAcrossInstances()
    {
        var provider = new ControllableProvider();
        await using var engine = new MonitoringEngine(provider);
        var store = new AlertRulesStore(TempStorePath()) { PersistenceEnabled = true };

        AlertsViewModel first = BuildAlerts(engine, store);
        first.SelectedSensor = first.Sensors[0];
        first.ThresholdText = "80";
        first.DurationSeconds = 5;
        first.AddRuleCommand.Execute(null);
        Assert.Single(first.Rules);

        // A fresh view-model over the same store rehydrates the saved rule.
        AlertsViewModel second = BuildAlerts(engine, store);

        Assert.Single(second.Rules);
        Assert.Equal(80f, second.Rules[0].Rule.Threshold);
        Assert.Equal(TimeSpan.FromSeconds(5), second.Rules[0].Rule.Duration);
    }

    /// <summary>A one-sensor provider whose reading is set by the test between passes.</summary>
    private sealed class ControllableProvider : ISensorProvider
    {
        private readonly HardwareItem _device =
            new HardwareItem("cpu", "CPU", HardwareKind.Cpu)
                .AddSensor(new Sensor("cpu/pkg/temp", "CPU Package", SensorKind.Temperature));

        public float NextValue { get; set; }

        public string Name => "Controllable";

        public bool IsAvailable => true;

        public bool RequiresElevation => false;

        public bool IsSimulated => true;

        public bool IsElevated => true;

        public bool HasLowLevelSensors => true;

        public IReadOnlyList<HardwareItem> GetHardware() => [_device];

        public IReadOnlyList<SensorReading> Sample() => [new SensorReading("cpu/pkg/temp", NextValue)];

        public void Dispose()
        {
        }
    }
}
