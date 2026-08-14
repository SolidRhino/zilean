namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Constant strings used for configuration file paths and section names.
/// </summary>
public static class ConfigurationLiterals
{
    /// <summary>
    /// Subdirectory (relative to <c>AppContext.BaseDirectory</c>) where configuration files are stored.
    /// </summary>
    public const string ConfigurationFolder = "data";

    /// <summary>
    /// Filename for the main Zilean settings JSON file.
    /// </summary>
    public const string SettingsConfigFilename = "settings.json";

    /// <summary>
    /// Filename for the Serilog logging configuration JSON file.
    /// </summary>
    public const string LoggingConfigFilename = "logging.json";

    /// <summary>
    /// Top-level JSON section name from which <see cref="ZileanConfiguration"/> is bound.
    /// </summary>
    public const string MainSettingsSectionName = "Zilean";
}