using System.ComponentModel;
using ChipGauge.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// A single dashboard tile: a title, a primary sensor (big value + chart) and a
/// couple of secondary readouts. Its title is either a localized key or a literal
/// device name (e.g. a specific disk model).
/// </summary>
public sealed class DashboardTileViewModel : ObservableObject
{
    private readonly Localizer _localizer;
    private readonly string _titleKeyOrName;
    private readonly bool _literalTitle;

    public DashboardTileViewModel(
        Localizer localizer,
        string titleKeyOrName,
        string iconKey,
        SensorViewModel primary,
        IReadOnlyList<SensorViewModel> secondary,
        bool literalTitle)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(secondary);

        _localizer = localizer;
        _titleKeyOrName = titleKeyOrName;
        _literalTitle = literalTitle;
        IconKey = iconKey;
        Primary = primary;
        Secondary = secondary;

        localizer.PropertyChanged += OnLocalizerChanged;
    }

    /// <summary>Icon geometry key.</summary>
    public string IconKey { get; }

    /// <summary>The tile's headline sensor (drives the big value and the chart).</summary>
    public SensorViewModel Primary { get; }

    /// <summary>Additional readouts shown beneath the chart.</summary>
    public IReadOnlyList<SensorViewModel> Secondary { get; }

    /// <summary>The tile title (localized key or literal device name).</summary>
    public string Title => _literalTitle ? _titleKeyOrName : _localizer[_titleKeyOrName];

    /// <summary>Refreshes the primary and secondary sensors.</summary>
    public void Refresh()
    {
        Primary.Refresh();
        foreach (SensorViewModel sensor in Secondary)
        {
            sensor.Refresh();
        }
    }

    private void OnLocalizerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_literalTitle && e.PropertyName is "Item[]" or null)
        {
            OnPropertyChanged(nameof(Title));
        }
    }
}
