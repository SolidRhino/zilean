using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Zilean.Scraper.Features.Ingestion.Processing;

/// <summary>
/// Provides extension methods for <see cref="TorrentInfo"/> in the ingestion context.
/// </summary>
public static class TorrentInfoExtensions
{
    /// <summary>
    /// Determines whether the torrent's infohash is present in the blacklisted set.
    /// </summary>
    /// <param name="torrent">The torrent to check.</param>
    /// <param name="blacklistedItems">The set of blacklisted infohashes.</param>
    /// <returns><c>true</c> if the torrent is blacklisted; otherwise <c>false</c>.</returns>
    public static bool IsBlacklisted(this TorrentInfo torrent, HashSet<string> blacklistedItems) =>
        blacklistedItems.Any(x => x.Equals(torrent.InfoHash, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Filters out blacklisted torrents from the finalized collection and logs the count removed.
    /// </summary>
    /// <param name="finalizedTorrentsEnumerable">The finalized torrents to filter.</param>
    /// <param name="parsedTorrents">The originally parsed torrents, used to count blacklisted matches.</param>
    /// <param name="blacklistedHashes">The set of blacklisted infohashes.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="processedCount">The counter tracking removed entries.</param>
    /// <returns>The filtered enumerable with blacklisted torrents removed.</returns>
    public static IEnumerable<TorrentInfo> FilterBlacklistedTorrents(this IEnumerable<TorrentInfo> finalizedTorrentsEnumerable,
        List<TorrentInfo> parsedTorrents, HashSet<string> blacklistedHashes, ZileanConfiguration configuration, ILogger logger,
        ProcessedCounts processedCount)
    {
        if (blacklistedHashes.Count <= 0)
        {
            return finalizedTorrentsEnumerable;
        }

        finalizedTorrentsEnumerable = finalizedTorrentsEnumerable.Where(t => !blacklistedHashes.Contains(t.InfoHash));
        var blacklistedCount = parsedTorrents.Count(x => blacklistedHashes.Contains(x.InfoHash));
        logger.LogInformation("Filtered out {Count} blacklisted torrents", blacklistedCount);
        processedCount.AddBlacklistedRemoved(blacklistedCount);

        return finalizedTorrentsEnumerable;
    }
}
