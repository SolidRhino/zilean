namespace Zilean.Database.Services;

/// <summary>
/// Provides IMDb matching operations for torrent entries using Lucene or FuzzySharp strategies.
/// </summary>
public interface IImdbMatchingService
{
    /// <summary>
    /// Matches IMDb identifiers for a batch of torrent entries.
    /// </summary>
    /// <param name="batch">The torrent entries to match against IMDb metadata.</param>
    /// <returns>A concurrent queue of matched <see cref="TorrentInfo"/> entries.</returns>
    Task<ConcurrentQueue<TorrentInfo>> MatchImdbIdsForBatchAsync(IEnumerable<TorrentInfo> batch);

    /// <summary>
    /// Loads IMDb metadata into the in-memory matching index.
    /// </summary>
    /// <returns>A task representing the asynchronous population operation.</returns>
    Task PopulateImdbData();

    /// <summary>
    /// Releases the in-memory IMDb matching index and associated resources.
    /// </summary>
    void DisposeImdbData();
}
