namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Root configuration POCO for Zilean, bound from the <c>Zilean</c> section of
/// <c>settings.json</c> and <c>Zilean__*</c> environment variables.
/// </summary>
public class ZileanConfiguration
{
    private static readonly JsonSerializerOptions? _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
    };

    /// <summary>
    /// Syncfusion license key. Set via the <c>Zilean__SyncfusionLicense</c> env var.
    /// If unset, Syncfusion runs in community/no-license mode (components remain
    /// functional but may display a license banner).
    /// </summary>
    public string? SyncfusionLicense { get; set; }

    /// <summary>
    /// API key for authenticating Torznab and internal API requests.
    /// Set via the <c>Zilean__ApiKey</c> env var; auto-generated if unset.
    /// </summary>
    public string? ApiKey { get; set; } = Utilities.ApiKey.Generate();

    /// <summary>
    /// Indicates whether Zilean is starting for the first time (triggers setup/init logic).
    /// Set via the <c>Zilean__FirstRun</c> env var.
    /// </summary>
    public bool FirstRun { get; set; } = true;

    /// <summary>
    /// Enables the built-in Blazor dashboard. Set via the <c>Zilean__EnableDashboard</c> env var.
    /// </summary>
    public bool EnableDashboard { get; set; } = false;

    /// <summary>
    /// DebridMediaManager scraping and endpoint configuration.
    /// Set via the <c>Zilean__Dmm</c> env var section.
    /// </summary>
    public DmmConfiguration Dmm { get; set; } = new();

    /// <summary>
    /// Torznab API endpoint configuration. Set via the <c>Zilean__Torznab</c> env var section.
    /// </summary>
    public TorznabConfiguration Torznab { get; set; } = new();

    /// <summary>
    /// PostgreSQL database connection configuration.
    /// Set via the <c>Zilean__Database</c> env var section or <c>POSTGRES_*</c> env vars.
    /// </summary>
    public DatabaseConfiguration Database { get; set; } = new();

    /// <summary>
    /// Torrent hash lookup endpoint configuration.
    /// Set via the <c>Zilean__Torrents</c> env var section.
    /// </summary>
    public TorrentsConfiguration Torrents { get; set; } = new();

    /// <summary>
    /// IMDb import and matching configuration. Set via the <c>Zilean__Imdb</c> env var section.
    /// </summary>
    public ImdbConfiguration Imdb { get; set; } = new();

    /// <summary>
    /// Generic ingestion (Zurg/Zilean/Generic instances) configuration.
    /// Set via the <c>Zilean__Ingestion</c> env var section.
    /// </summary>
    public IngestionConfiguration Ingestion { get; set; } = new();

    /// <summary>
    /// Torrent title parsing configuration. Set via the <c>Zilean__Parsing</c> env var section.
    /// </summary>
    public ParsingConfiguration Parsing { get; set; } = new();

    /// <summary>
    /// Ensures the <c>settings.json</c> file exists in the configuration directory,
    /// creating it with default values if absent.
    /// </summary>
    public static void EnsureExists()
    {
        var settingsFilePath = Path.Combine(AppContext.BaseDirectory, ConfigurationLiterals.ConfigurationFolder, ConfigurationLiterals.SettingsConfigFilename);
        if (!File.Exists(settingsFilePath))
        {
            File.WriteAllText(settingsFilePath, DefaultConfigurationContents());
        }
    }

    /// <summary>
    /// Validates the configuration and returns a list of error messages. Empty list means valid.
    /// </summary>
    /// <returns>A list of validation error messages; empty if the configuration is valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (Dmm.MaxFilteredResults <= 0)
        {
            errors.Add("Dmm.MaxFilteredResults must be greater than 0");
        }

        if (Dmm.MinimumScoreMatch is < 0 or > 1)
        {
            errors.Add("Dmm.MinimumScoreMatch must be between 0 and 1");
        }

        if (Dmm.MinimumReDownloadIntervalMinutes < 0)
        {
            errors.Add("Dmm.MinimumReDownloadIntervalMinutes must be non-negative");
        }

        if (!IsValidCronExpression(Dmm.ScrapeSchedule))
        {
            errors.Add($"Dmm.ScrapeSchedule '{Dmm.ScrapeSchedule}' is not a valid cron expression");
        }

        if (!IsValidCronExpression(Ingestion.ScrapeSchedule))
        {
            errors.Add($"Ingestion.ScrapeSchedule '{Ingestion.ScrapeSchedule}' is not a valid cron expression");
        }

        if (Parsing.BatchSize <= 0)
        {
            errors.Add("Parsing.BatchSize must be greater than 0");
        }

        if (string.IsNullOrWhiteSpace(Database.ConnectionString))
        {
            errors.Add("Database.ConnectionString is empty — check POSTGRES_* or Zilean__Database__ConnectionString env vars");
        }

        return errors;
    }

    private static bool IsValidCronExpression(string? cron)
    {
        if (string.IsNullOrWhiteSpace(cron))
        {
            return false;
        }

        var parts = cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 5;
    }

    private static string DefaultConfigurationContents()
    {
        var mainSettings = new Dictionary<string, object>
        {
            [ConfigurationLiterals.MainSettingsSectionName] = new ZileanConfiguration(),
        };

        return JsonSerializer.Serialize(mainSettings, _jsonSerializerOptions);
    }
}