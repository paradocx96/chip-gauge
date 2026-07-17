using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChipGauge.App.Services;

/// <summary>The user's theme preference.</summary>
public enum AppTheme
{
    /// <summary>Follow the operating system.</summary>
    System,

    /// <summary>Always light.</summary>
    Light,

    /// <summary>Always dark.</summary>
    Dark,
}

/// <summary>
/// Applies the selected <see cref="AppTheme"/> to the Avalonia application. Guards
/// against a null <see cref="Application.Current"/> so it is safe to construct in
/// unit tests that do not spin up the UI.
/// </summary>
public sealed partial class ThemeService : ObservableObject
{
    [ObservableProperty]
    private AppTheme _theme = AppTheme.System;

    partial void OnThemeChanged(AppTheme value) => Apply();

    /// <summary>Applies the current <see cref="Theme"/> to the running application.</summary>
    public void Apply()
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = Theme switch
            {
                AppTheme.Light => ThemeVariant.Light,
                AppTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
        }
    }
}
