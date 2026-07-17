using System.ComponentModel;
using ChipGauge.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChipGauge.App.ViewModels;

/// <summary>
/// Base class for a navigable page. Carries its localized title and icon key, and
/// receives a per-second <see cref="OnTick"/> while it is the visible page.
/// </summary>
public abstract class PageViewModel : ObservableObject
{
    protected PageViewModel(Localizer localizer, string titleKey, string iconKey)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        Localizer = localizer;
        TitleKey = titleKey;
        IconKey = iconKey;
        localizer.PropertyChanged += OnLocalizerChanged;
    }

    /// <summary>The shared localizer.</summary>
    protected Localizer Localizer { get; }

    /// <summary>Resource key for the page title.</summary>
    public string TitleKey { get; }

    /// <summary>Key of the icon geometry in <c>Icons.axaml</c>.</summary>
    public string IconKey { get; }

    /// <summary>The localized page title (updates live on language change).</summary>
    public string Title => Localizer[TitleKey];

    /// <summary>Refreshes live values; called each second while this page is visible.</summary>
    public abstract void OnTick();

    private void OnLocalizerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is "Item[]" or null)
        {
            OnPropertyChanged(nameof(Title));
        }
    }
}
