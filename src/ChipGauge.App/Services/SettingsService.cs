using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChipGauge.App.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON under
/// <c>%AppData%/ChipGauge/settings.json</c>. Saving is a no-op until
/// <see cref="PersistenceEnabled"/> is turned on (the app enables it after applying
/// the loaded settings), so building the service graph in tests never touches disk.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;

    /// <summary>Creates a service writing to the default per-user location.</summary>
    public SettingsService()
        : this(DefaultPath())
    {
    }

    /// <summary>Creates a service writing to an explicit path (used by tests).</summary>
    public SettingsService(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        _path = path;
    }

    /// <summary>When false, <see cref="Save"/> does nothing (default).</summary>
    public bool PersistenceEnabled { get; set; }

    /// <summary>The resolved settings file path.</summary>
    public string Path => _path;

    /// <summary>Reads the settings file (if any) into <paramref name="settings"/>; leaves defaults on any error.</summary>
    public void LoadInto(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        try
        {
            if (!File.Exists(_path))
            {
                return;
            }

            SettingsDto? dto = JsonSerializer.Deserialize<SettingsDto>(File.ReadAllText(_path), JsonOptions);
            if (dto is null)
            {
                return;
            }

            settings.Theme = dto.Theme;
            settings.Language = string.IsNullOrWhiteSpace(dto.Language) ? "en" : dto.Language;
            settings.TemperatureUnit = dto.TemperatureUnit;
            settings.PollingSeconds = dto.PollingSeconds is >= 0.5 and <= 10 ? dto.PollingSeconds : 1.0;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupt or unreadable file — fall back to defaults.
        }
    }

    /// <summary>Writes the settings to disk (best-effort) when persistence is enabled.</summary>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!PersistenceEnabled)
        {
            return;
        }

        try
        {
            string? directory = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var dto = new SettingsDto
            {
                Theme = settings.Theme,
                Language = settings.Language,
                TemperatureUnit = settings.TemperatureUnit,
                PollingSeconds = settings.PollingSeconds,
            };
            File.WriteAllText(_path, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort persistence — ignore write failures.
        }
    }

    private static string DefaultPath() => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChipGauge",
        "settings.json");

    private sealed class SettingsDto
    {
        public AppTheme Theme { get; set; }

        public string? Language { get; set; }

        public TemperatureUnit TemperatureUnit { get; set; }

        public double PollingSeconds { get; set; } = 1.0;
    }
}
