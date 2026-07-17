using System.Text.Json;
using System.Text.Json.Serialization;
using ChipGauge.Core.Alerts;

namespace ChipGauge.App.Services;

/// <summary>
/// Loads and saves the user's <see cref="AlertRule"/>s as JSON under
/// <c>%AppData%/ChipGauge/alerts.json</c>. Like <see cref="SettingsService"/>,
/// saving is a no-op until <see cref="PersistenceEnabled"/> is turned on, so the
/// service graph in tests never touches disk.
/// </summary>
public sealed class AlertRulesStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;

    /// <summary>Creates a store writing to the default per-user location.</summary>
    public AlertRulesStore()
        : this(DefaultPath())
    {
    }

    /// <summary>Creates a store writing to an explicit path (used by tests).</summary>
    public AlertRulesStore(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        _path = path;
    }

    /// <summary>When false, <see cref="Save"/> does nothing (default).</summary>
    public bool PersistenceEnabled { get; set; }

    /// <summary>Reads the saved rules (empty on any error or when the file is absent).</summary>
    public IReadOnlyList<AlertRule> Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return [];
            }

            List<AlertRuleDto>? dtos = JsonSerializer.Deserialize<List<AlertRuleDto>>(File.ReadAllText(_path), JsonOptions);
            if (dtos is null)
            {
                return [];
            }

            var rules = new List<AlertRule>(dtos.Count);
            foreach (AlertRuleDto dto in dtos)
            {
                if (string.IsNullOrEmpty(dto.Id) || string.IsNullOrEmpty(dto.SensorId))
                {
                    continue;
                }

                rules.Add(new AlertRule(
                    dto.Id,
                    dto.SensorId,
                    dto.Comparison,
                    dto.Threshold,
                    TimeSpan.FromSeconds(Math.Max(0, dto.DurationSeconds)),
                    dto.Message));
            }

            return rules;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>Writes the rules to disk (best-effort) when persistence is enabled.</summary>
    public void Save(IEnumerable<AlertRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
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

            List<AlertRuleDto> dtos =
            [
                .. rules.Select(r => new AlertRuleDto
                {
                    Id = r.Id,
                    SensorId = r.SensorId,
                    Comparison = r.Comparison,
                    Threshold = r.Threshold,
                    DurationSeconds = r.Duration.TotalSeconds,
                    Message = r.Message,
                }),
            ];
            File.WriteAllText(_path, JsonSerializer.Serialize(dtos, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort persistence — ignore write failures.
        }
    }

    private static string DefaultPath() => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChipGauge",
        "alerts.json");

    private sealed class AlertRuleDto
    {
        public string? Id { get; set; }

        public string? SensorId { get; set; }

        public AlertComparison Comparison { get; set; }

        public float Threshold { get; set; }

        public double DurationSeconds { get; set; }

        public string? Message { get; set; }
    }
}
