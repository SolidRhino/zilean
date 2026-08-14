using Microsoft.Extensions.Configuration;

namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Extension methods for registering Serilog logging configuration from JSON files.
/// </summary>
public static class LoggingConfiguration
{
    private const string DefaultLoggingContents =
        """
        {
          "Serilog": {
            "MinimumLevel": {
              "Default": "Information",
              "Override": {
                "Microsoft": "Warning",
                "System": "Warning",
                "System.Net.Http.HttpClient.Scraper.LogicalHandler": "Warning",
                "System.Net.Http.HttpClient.Scraper.ClientHandler": "Warning",
                "Microsoft.AspNetCore.Hosting.Diagnostics": "Error",
                "Microsoft.AspNetCore.DataProtection": "Error"
              }
            }
          }
        }
        """;

    /// <summary>
    /// Adds the Serilog logging JSON file to the configuration builder, creating a
    /// default <c>logging.json</c> if one does not already exist.
    /// </summary>
    /// <param name="configuration">The configuration builder to extend.</param>
    /// <param name="configurationFolderPath">Path to the directory where <c>logging.json</c> is stored.</param>
    /// <returns>The same <paramref name="configuration"/> builder instance for chaining.</returns>
    public static IConfigurationBuilder AddLoggingConfiguration(this IConfigurationBuilder configuration, string configurationFolderPath)
    {
        EnsureExists(configurationFolderPath);

        configuration.AddJsonFile(ConfigurationLiterals.LoggingConfigFilename, false, false);

        return configuration;
    }

    private static void EnsureExists(string configurationFolderPath)
    {
        var loggingPath = Path.Combine(configurationFolderPath, ConfigurationLiterals.LoggingConfigFilename);
        if (!File.Exists(loggingPath))
        {
            File.WriteAllText(loggingPath, DefaultLoggingContents);
        }
    }
}