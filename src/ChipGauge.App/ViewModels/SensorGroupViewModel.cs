using System.ComponentModel;
using ChipGauge.App.Services;
using ChipGauge.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// A set of a device's sensors that share a <see cref="SensorKind"/> (e.g. all
/// temperatures), shown under a localized heading. Grouping turns a device's long
/// flat sensor list into a few readable sections.
/// </summary>
public sealed class SensorGroupViewModel : ObservableObject
{
    private readonly Localizer _localizer;
    private readonly string _titleKey;

    public SensorGroupViewModel(Localizer localizer, SensorKind kind, IReadOnlyList<SensorViewModel> sensors)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(sensors);
        _localizer = localizer;
        _titleKey = KeyFor(kind);
        Sensors = sensors;
        localizer.PropertyChanged += OnLocalizerChanged;
    }

    /// <summary>The sensors in this group.</summary>
    public IReadOnlyList<SensorViewModel> Sensors { get; }

    /// <summary>Localized group heading.</summary>
    public string Title => _localizer[_titleKey];

    /// <summary>Maps a sensor kind to its group heading resource key.</summary>
    public static string KeyFor(SensorKind kind) => kind switch
    {
        SensorKind.Load => "SensorGroup_Load",
        SensorKind.Clock => "SensorGroup_Clock",
        SensorKind.Temperature => "SensorGroup_Temperature",
        SensorKind.Voltage => "SensorGroup_Voltage",
        SensorKind.Current => "SensorGroup_Current",
        SensorKind.Power => "SensorGroup_Power",
        SensorKind.Fan => "SensorGroup_Fan",
        SensorKind.Flow => "SensorGroup_Flow",
        SensorKind.Throughput => "SensorGroup_Throughput",
        SensorKind.Level => "SensorGroup_Level",
        SensorKind.Data => "SensorGroup_Data",
        SensorKind.Control => "SensorGroup_Control",
        SensorKind.Factor => "SensorGroup_Factor",
        _ => "SensorGroup_Other",
    };

    /// <summary>Display order for a kind, so groups read in a sensible sequence.</summary>
    public static int OrderFor(SensorKind kind) => kind switch
    {
        SensorKind.Load => 0,
        SensorKind.Temperature => 1,
        SensorKind.Clock => 2,
        SensorKind.Power => 3,
        SensorKind.Voltage => 4,
        SensorKind.Current => 5,
        SensorKind.Fan => 6,
        SensorKind.Throughput => 7,
        SensorKind.Data => 8,
        SensorKind.Level => 9,
        SensorKind.Flow => 10,
        SensorKind.Control => 11,
        SensorKind.Factor => 12,
        _ => 99,
    };

    private void OnLocalizerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is "Item[]" or null or "")
        {
            OnPropertyChanged(nameof(Title));
        }
    }
}
