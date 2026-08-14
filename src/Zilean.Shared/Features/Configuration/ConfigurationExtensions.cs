using Microsoft.Extensions.Configuration;

namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Extension methods for registering and retrieving Zilean configuration from
/// <c>IConfigurationBuilder</c> and <c>IConfiguration</c>.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds Zilean configuration sources (settings JSON, logging JSON, environment variables)
    /// to the builder, ensuring the configuration directory and default files exist.
    /// </summary>
    /// <param name="configuration">The configuration builder to extend.</param>
    /// <returns>The same <paramref name="configuration"/> builder instance for chaining.</returns>
    public static IConfigurationBuilder AddConfigurationFiles(this IConfigurationBuilder configuration)
    {
        var configurationFolderPath = Path.Combine(AppContext.BaseDirectory, ConfigurationLiterals.ConfigurationFolder);

        EnsureConfigurationDirectoryExists(configurationFolderPath);

        ZileanConfiguration.EnsureExists();

        configuration.SetBasePath(configurationFolderPath);
        configuration.AddLoggingConfiguration(configurationFolderPath);
        configuration.AddJsonFile(ConfigurationLiterals.SettingsConfigFilename, false, false);
        configuration.AddEnvironmentVariables();

        return configuration;
    }

    /// <summary>
    /// Binds the <c>Zilean</c> configuration section to a <see cref="ZileanConfiguration"/>
    /// instance, returning a default instance if the section is absent.
    /// </summary>
    /// <param name="configuration">The root configuration to read from.</param>
    /// <returns>A populated <see cref="ZileanConfiguration"/> instance.</returns>
    public static ZileanConfiguration GetZileanConfiguration(this IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigurationLiterals.MainSettingsSectionName);
        return NormalizeSyncfusionLicense(section.Get<ZileanConfiguration>());
    }

    private static ZileanConfiguration NormalizeSyncfusionLicense(ZileanConfiguration? config)
    {
        if (config is null)
        {
            return new ZileanConfiguration();
        }

        return config;
    }

    private static void EnsureConfigurationDirectoryExists(string configurationFolderPath)
    {
        if (!Directory.Exists(configurationFolderPath))
        {
            Directory.CreateDirectory(configurationFolderPath);
        }
    }
}