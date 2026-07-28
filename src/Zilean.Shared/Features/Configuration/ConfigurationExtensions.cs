using Microsoft.Extensions.Configuration;

namespace Zilean.Shared.Features.Configuration;

public static class ConfigurationExtensions
{
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

        if (string.IsNullOrWhiteSpace(config.SyncfusionLicense))
        {
            config.SyncfusionLicense = ZileanConfiguration.DefaultSyncfusionLicense;
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
