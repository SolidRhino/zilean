namespace Zilean.Scraper.Features.Commands;

/// <summary>
/// Scrapes generic ingestion endpoints (Zurg, other Zilean instances) and upserts torrent metadata.
/// </summary>
/// <param name="genericIngestion">The generic ingestion scraping service that performs the sync.</param>
public class GenericSyncCommand(GenericIngestionScraping genericIngestion) : AsyncCommand
{
    /// <summary>
    /// Executes the generic sync by delegating to <see cref="GenericIngestionScraping.Execute(CancellationToken)"/>.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <returns>A task representing the async operation; zero on success.</returns>
    public override Task<int> ExecuteAsync(CommandContext context) =>
        genericIngestion.Execute(CancellationToken.None);
}
