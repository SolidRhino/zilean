namespace Zilean.ApiService.Features.Bootstrapping;

/// <summary>
/// Hosted service that persists the current <see cref="ZileanConfiguration"/> to <c>settings.json</c> on startup, regenerates the API key when the <c>ZILEAN__NEW__API__KEY</c> env var is set, and logs the key on first run.
/// </summary>
/// <param name="configuration">The Zilean configuration to persist and inspect.</param>
/// <param name="logger">The logger for configuration update diagnostics.</param>
public class ConfigurationUpdaterService(ZileanConfiguration configuration, ILogger<ConfigurationUpdaterService> logger) : IHostedService
{
    private const string ResetApiKeyEnvVar = "ZILEAN__NEW__API__KEY";

    /// <summary>
    /// Clears the first-run flag, optionally regenerates the API key if the <c>ZILEAN__NEW__API__KEY</c> env var is set, and writes the configuration to <c>settings.json</c>.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A task that completes when the configuration file has been written.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        bool firstRun = configuration.FirstRun;

        if (firstRun)
        {
            configuration.FirstRun = false;
        }

        if (Environment.GetEnvironmentVariable(ResetApiKeyEnvVar) is "1" or "true")
        {
            configuration.ApiKey = ApiKey.Generate();
            logger.LogInformation("API Key regenerated: {ApiKeyPrefix}...", configuration.ApiKey[..Math.Min(6, configuration.ApiKey.Length)]);
            logger.LogInformation("Please keep this key safe and secure.");
        }

        var configurationFolderPath = Path.Combine(AppContext.BaseDirectory, ConfigurationLiterals.ConfigurationFolder);
        var configurationFilePath = Path.Combine(configurationFolderPath, ConfigurationLiterals.SettingsConfigFilename);

        var configWrapper = new Dictionary<string, object>
        {
            [ConfigurationLiterals.MainSettingsSectionName] = configuration,
        };

        await File.WriteAllTextAsync(configurationFilePath,
            JsonSerializer.Serialize(configWrapper,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null,
                }), cancellationToken);

        if (firstRun)
        {
            logger.LogInformation("Zilean API Key: {ApiKeyPrefix}... (full key in settings.json)", configuration.ApiKey[..Math.Min(6, configuration.ApiKey.Length)]);
            logger.LogInformation("Please keep this key safe and secure.");
        }
    }

    /// <summary>
    /// No-op; the configuration update is a startup-only operation.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}