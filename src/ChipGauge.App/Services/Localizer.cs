using System.Collections;
using System.Globalization;
using System.Resources;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChipGauge.App.Services;

/// <summary>
/// Runtime localization backed by embedded <c>.resx</c> resources. Every string is
/// published into the application resource dictionary under an <c>"L."</c> prefix;
/// XAML binds to it through the <see cref="Markup.LocalizeExtension"/> (a
/// <c>DynamicResource</c>), so switching language updates all bound text live —
/// no restart. View-models that need a string directly use the <see cref="this"/>
/// indexer and observe <see cref="ObservableObject.PropertyChanged"/>.
/// </summary>
public sealed partial class Localizer : ObservableObject
{
    /// <summary>Prefix used for published resource keys.</summary>
    public const string ResourcePrefix = "L.";

    /// <summary>The languages ChipGauge ships with.</summary>
    public static readonly IReadOnlyList<LanguageOption> Languages =
    [
        new LanguageOption("en", "English"),
        new LanguageOption("de", "Deutsch"),
    ];

    private readonly ResourceManager _resources = new(
        "ChipGauge.App.Resources.Strings", typeof(Localizer).Assembly);

    private CultureInfo _culture = CultureInfo.CurrentUICulture;

    public Localizer() => Instance = this;

    /// <summary>The most recently constructed localizer (used by the XAML markup extension).</summary>
    public static Localizer? Instance { get; private set; }

    /// <summary>The active UI culture.</summary>
    public CultureInfo Culture => _culture;

    /// <summary>The active language code (e.g. "en", "de").</summary>
    public string CurrentLanguage => _culture.TwoLetterISOLanguageName;

    /// <summary>Looks up a localized string by key, falling back to the key itself.</summary>
    public string this[string key] => _resources.GetString(key, _culture) ?? key;

    /// <summary>
    /// Publishes every string into <see cref="Application.Current"/>'s resources under
    /// the <see cref="ResourcePrefix"/>. Safe to call when there is no application
    /// (e.g. in unit tests) — it simply does nothing.
    /// </summary>
    public void PublishResources()
    {
        if (Application.Current is not { } app)
        {
            return;
        }

        ResourceSet? set = _resources.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);
        if (set is null)
        {
            return;
        }

        foreach (DictionaryEntry entry in set)
        {
            if (entry.Key is string key)
            {
                app.Resources[ResourcePrefix + key] = this[key];
            }
        }
    }

    /// <summary>Switches the active language and refreshes every bound string.</summary>
    public void SetLanguage(string languageCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(languageCode);
        var culture = new CultureInfo(languageCode);
        if (culture.TwoLetterISOLanguageName == _culture.TwoLetterISOLanguageName)
        {
            return;
        }

        _culture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;

        PublishResources();                 // refreshes DynamicResource bindings
        OnPropertyChanged("Item[]");         // refreshes indexer-based view-model text
        OnPropertyChanged(nameof(Culture));
        OnPropertyChanged(nameof(CurrentLanguage));
    }
}

/// <summary>A selectable UI language.</summary>
/// <param name="Code">Two-letter ISO code.</param>
/// <param name="DisplayName">Native display name.</param>
public sealed record LanguageOption(string Code, string DisplayName);
