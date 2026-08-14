namespace Zilean.Database.Services;

/// <summary>
/// Provides search and storage operations for <see cref="TorrentInfo"/> entries.
/// </summary>
public interface ITorrentInfoService
{
    /// <summary>
    /// Bulk-stores torrent info entries, upserting on InfoHash conflict.
    /// </summary>
    /// <param name="torrents">The torrent entries to store.</param>
    /// <param name="batchSize">The number of entries per upsert batch.</param>
    /// <returns>A <see cref="StoreResult"/> with counts and timing metrics.</returns>
    Task<StoreResult> StoreTorrentInfo(List<TorrentInfo> torrents, int batchSize = 5000);

    /// <summary>
    /// Searches for torrents matching the given title only (no filter criteria).
    /// </summary>
    /// <param name="query">The title search query.</param>
    /// <returns>An array of matching <see cref="TorrentInfo"/> entries.</returns>
    Task<TorrentInfo[]> SearchForTorrentInfoByOnlyTitle(string query);

    /// <summary>
    /// Searches for torrents matching the given filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria to apply.</param>
    /// <param name="limit">Optional maximum number of results to return.</param>
    /// <returns>An array of matching <see cref="TorrentInfo"/> entries.</returns>
    Task<TorrentInfo[]> SearchForTorrentInfoFiltered(TorrentInfoFilter filter, int? limit = null);

    /// <summary>
    /// Returns the set of info hashes already present in the database.
    /// </summary>
    /// <param name="infoHashes">The info hashes to check for existence.</param>
    /// <returns>A set of info hashes that already exist in the database.</returns>
    Task<HashSet<string>> GetExistingInfoHashesAsync(List<string> infoHashes);

    /// <summary>
    /// Returns the set of info hashes currently on the blacklist.
    /// </summary>
    /// <returns>A set of blacklisted info hashes.</returns>
    Task<HashSet<string>> GetBlacklistedItems();

    /// <summary>
    /// Runs VACUUM ANALYZE on the Torrents table to maintain index statistics.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task VaccumTorrentsIndexes(CancellationToken cancellationToken);
}
