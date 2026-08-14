namespace Zilean.Scraper.Features.Bootstrapping;

/// <summary>
/// Hosted service that applies EF Core migrations on startup and optionally loads IMDb metadata.
/// </summary>
/// <param name="metadataLoader">Loads IMDb metadata files into the database.</param>
/// <param name="logger">Logger for diagnostic output.</param>
/// <param name="dbContextFactory">Factory for creating <see cref="ZileanDbContext"/> instances.</param>
/// <param name="configuration">Application configuration controlling IMDb import matching.</param>
public class EnsureMigrated(ImdbMetadataLoader metadataLoader, ILogger<EnsureMigrated> logger, IDbContextFactory<ZileanDbContext> dbContextFactory, ZileanConfiguration configuration) : IHostedService
{
    /// <summary>
    /// Applies pending database migrations and, if enabled, loads IMDb metadata for import matching.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Applying Migrations...");
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
        logger.LogInformation("Migrations Applied.");

        if (configuration.Imdb.EnableImportMatching)
        {
            var imdbLoadedResult = await metadataLoader.Execute(cancellationToken);

            if (imdbLoadedResult == 1)
            {
                throw new InvalidOperationException("IMDB metadata load failed. Cannot proceed with scraping.");
            }
        }
    }

    /// <summary>
    /// No-op stop handler; no cleanup is required when the service stops.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
