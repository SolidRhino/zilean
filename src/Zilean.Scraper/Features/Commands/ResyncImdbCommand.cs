namespace Zilean.Scraper.Features.Commands;

/// <summary>
/// Re-imports IMDb metadata and optionally re-matches torrents to IMDb identifiers.
/// </summary>
/// <param name="imdbLoader">Loads IMDb metadata files into the database.</param>
/// <param name="torrentInfoService">Service for maintaining torrent indexes.</param>
/// <param name="imdbMatchingService">Service for matching torrents to IMDb identifiers.</param>
/// <param name="dbContext">Database context for querying and updating torrents.</param>
/// <param name="serviceProvider">Service provider for creating scoped contexts.</param>
/// <param name="logger">Logger for diagnostic output.</param>
public class ResyncImdbCommand(
    ImdbMetadataLoader imdbLoader,
    ITorrentInfoService torrentInfoService,
    IImdbMatchingService imdbMatchingService,
    ZileanDbContext dbContext,
    IServiceProvider serviceProvider,
    ILogger<ResyncImdbCommand> logger) : AsyncCommand<ResyncImdbCommand.ResyncImdbCommandSettings>
{
    /// <summary>
    /// Settings for <see cref="ResyncImdbCommand"/>.
    /// </summary>
    public sealed class ResyncImdbCommandSettings : CommandSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to skip the 14-day date check on IMDb imports and force import.
        /// </summary>
        [CommandOption("-s|--skip-last-import")]
        [Description("Skip the date check on imdb imports (last 14 days) and force it to import.")]
        [DefaultValue(false)]
        public bool SkipLastImport { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to match IMDb identifiers for torrents missing them.
        /// </summary>
        [CommandOption("-t|--retag-missing-imdbs")]
        [Description("Will attempt to match IMDB ids for anything that is missing them in the database.")]
        [DefaultValue(false)]
        public bool RetagMissingImdbs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to match IMDb identifiers for all torrents.
        /// </summary>
        [CommandOption("-a|--retag-all-imdbs")]
        [Description("Will attempt to match IMDB ids for all torrents.")]
        [DefaultValue(false)]
        public bool RetagAllImdbs { get; set; }
    }

    /// <summary>
    /// Re-imports IMDb metadata and optionally re-matches torrents to IMDb identifiers based on settings.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>A task representing the async operation; zero on success, one on failure.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, ResyncImdbCommandSettings settings)
    {
        if (settings is {RetagAllImdbs: true, RetagMissingImdbs: true})
        {
            logger.LogError("Cannot use both --retag-missing-imdbs and --retag-all-imdbs at the same time");
            return 1;
        }

        var result = await imdbLoader.Execute(CancellationToken.None, skipLastImport: settings.SkipLastImport);

        if (result != 0)
        {
            return result;
        }

        try
        {
            if (settings.RetagMissingImdbs)
            {
                await HandleRetagging(all: false);
                return 0;
            }

            if (settings.RetagAllImdbs)
            {
                await HandleRetagging(all: true);
                return 0;
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occurred during ResyncImdbCommand");
            result = 1;
        }

        return result;
    }

    private async Task HandleRetagging(bool all = false)
    {
        var torrents = dbContext.Torrents.AsNoTracking()
            .Where(x => x.Category != "xxx");

        if (!all)
        {
            torrents = torrents.Where(x => x.ImdbId == null);
        }

        var processableTorrents = await torrents.ToListAsync();

        logger.LogInformation("Found {TorrentCount} torrents", processableTorrents.Count);

        if (processableTorrents.Count > 0)
        {
            // Drop any in-memory state from prior ingestion runs so the resync
            // sees the freshly-loaded IMDb data (imdbLoader.Execute above just
            // refreshed the ImdbFiles table). Without this, the singleton matcher
            // would return early from PopulateImdbData and reuse stale entries.
            imdbMatchingService.DisposeImdbData();
            await imdbMatchingService.PopulateImdbData();

            logger.LogInformation("Starting to process torrents...");

            var updatedTorrents = await imdbMatchingService.MatchImdbIdsForBatchAsync(processableTorrents);

            imdbMatchingService.DisposeImdbData();

            logger.LogInformation("Updating {TorrentCount} torrents", updatedTorrents.Count);

            await using var scope = serviceProvider.CreateAsyncScope();
            var scopedDbContext = scope.ServiceProvider.GetRequiredService<ZileanDbContext>();

            scopedDbContext.AttachRange(updatedTorrents);
            scopedDbContext.UpdateRange(updatedTorrents);
            await scopedDbContext.SaveChangesAsync();

            logger.LogInformation("Finished processing torrents");
        }
        else
        {
            logger.LogInformation("No torrents found to match");
        }

        await torrentInfoService.VaccumTorrentsIndexes(CancellationToken.None);
    }
}
