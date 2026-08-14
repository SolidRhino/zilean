namespace Zilean.Scraper.Features.Imdb;

/// <summary>
/// Orchestrates IMDb metadata loading: checks the last import date, downloads
/// fresh data if needed, and imports it into the database.
/// </summary>
/// <param name="downloader">The IMDb file downloader.</param>
/// <param name="processor">The IMDb file processor.</param>
/// <param name="logger">The logger for diagnostic output.</param>
/// <param name="imdbFileService">The IMDb file service for import-status persistence.</param>
public class ImdbMetadataLoader(ImdbFileDownloader downloader, ImdbFileProcessor processor, ILogger<ImdbMetadataLoader> logger, ImdbFileService imdbFileService)
{
    /// <summary>
    /// Executes the IMDb metadata load workflow, skipping the import if the last
    /// import was less than 14 days ago unless <paramref name="skipLastImport"/> is set.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="skipLastImport">If <c>true</c>, skips the last-import staleness check.</param>
    /// <returns>0 on success, 1 on error or cancellation.</returns>
    public async Task<int> Execute(CancellationToken cancellationToken, bool skipLastImport = false)
    {
        try
        {
            if (!skipLastImport)
            {
                var imdbLastImport = await imdbFileService.GetImdbLastImportAsync(cancellationToken);

                if (imdbLastImport is not null)
                {
                    logger.LogInformation("Last import date: {LastImportDate}", imdbLastImport.OccuredAt);
                    if (DateTime.UtcNow - imdbLastImport.OccuredAt < TimeSpan.FromDays(14))
                    {
                        logger.LogInformation("Imdb Records import is not required as last import was less than 14 days ago");
                        return 0;
                    }
                }
            }

            var dataFile = await downloader.DownloadMetadataFile(cancellationToken);

            await processor.Import(dataFile, cancellationToken);

            logger.LogInformation("All IMDB records processed");

            logger.LogInformation("ImdbMetadataLoader Tasks Completed");

            return 0;
        }
        catch (TaskCanceledException)
        {
            logger.LogInformation("ImdbMetadataLoader Task Cancelled");
            return 1;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("ImdbMetadataLoader Task Cancelled");
            return 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during ImdbMetadataLoader Task");
            return 1;
        }
    }
}
