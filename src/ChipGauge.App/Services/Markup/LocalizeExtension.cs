using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using ChipGauge.App.Services;

namespace ChipGauge.App.Services.Markup;

/// <summary>
/// XAML markup extension for localized text: <c>{loc:Localize Nav_Dashboard}</c>.
/// It resolves to a <c>DynamicResource</c> for the published <c>"L.&lt;key&gt;"</c>
/// resource, so the text updates automatically whenever the language changes.
/// </summary>
public sealed class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension()
    {
    }

    public LocalizeExtension(string key) => Key = key;

    /// <summary>The resource key to resolve.</summary>
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new DynamicResourceExtension(Localizer.ResourcePrefix + Key).ProvideValue(serviceProvider);
}
