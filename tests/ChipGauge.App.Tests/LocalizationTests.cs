using System.Collections;
using System.Globalization;
using System.Resources;
using ChipGauge.App.Services;

namespace ChipGauge.App.Tests;

public sealed class LocalizationTests
{
    private static ResourceManager CreateManager() =>
        new("ChipGauge.App.Resources.Strings", typeof(Localizer).Assembly);

    [Fact]
    public void Localizer_ResolvesEnglishAndGerman()
    {
        var localizer = new Localizer();

        localizer.SetLanguage("en");
        Assert.Equal("Dashboard", localizer["Nav_Dashboard"]);

        localizer.SetLanguage("de");
        Assert.Equal("Übersicht", localizer["Nav_Dashboard"]);
        Assert.Equal("de", localizer.CurrentLanguage);
    }

    [Fact]
    public void Localizer_UnknownKey_FallsBackToKey()
    {
        var localizer = new Localizer();
        Assert.Equal("Missing_Key", localizer["Missing_Key"]);
    }

    [Fact]
    public void ShippedLanguages_AreEnglishAndGerman()
    {
        Assert.Equal(2, Localizer.Languages.Count);
        Assert.Contains(Localizer.Languages, l => l.Code == "en");
        Assert.Contains(Localizer.Languages, l => l.Code == "de");
    }

    [Fact]
    public void EveryEnglishKey_HasAGermanTranslation()
    {
        ResourceManager manager = CreateManager();
        ResourceSet neutral = manager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;
        ResourceSet german = manager.GetResourceSet(new CultureInfo("de"), createIfNotExists: true, tryParents: false)!;

        var missing = new List<string>();
        foreach (DictionaryEntry entry in neutral)
        {
            if (entry.Key is string key && german.GetString(key) is null)
            {
                missing.Add(key);
            }
        }

        Assert.True(missing.Count == 0, $"German translations missing for: {string.Join(", ", missing)}");
    }
}
