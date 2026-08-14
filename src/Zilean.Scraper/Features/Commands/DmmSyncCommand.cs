using Zilean.Scraper.Features.Ingestion.Dmm;

namespace Zilean.Scraper.Features.Commands;

/// <summary>
/// Downloads and processes the DMM hashlist, parsing entries and bulk-upserting torrent metadata.
/// </summary>
/// <param name="dmmScraping">The DMM scraping service that performs the sync.</param>
public class DmmSyncCommand(DmmScraping dmmScraping) : AsyncCommand
{
    /// <summary>
    /// Executes the DMM sync by delegating to <see cref="DmmScraping.Execute(CancellationToken)"/>.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <returns>A task representing the async operation; zero on success.</returns>
    public override Task<int> ExecuteAsync(CommandContext context) =>
        dmmScraping.Execute(CancellationToken.None);
}
