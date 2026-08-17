namespace Zilean.Database.Services;

/// <summary>
/// Provides query operations for cached torrent availability and streaming.
/// </summary>
public interface ITorrentsQueryService
{
    /// <summary>
    /// Checks which of the given info hashes are cached in the database.
    /// </summary>
    /// <param name="hashes">The info hashes to check.</param>
    /// <param name="maxHashes">The maximum number of hashes allowed.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>A list of <see cref="CachedItem"/> entries indicating cache status per hash.</returns>
    Task<IReadOnlyList<CachedItem>> CheckCachedAsync(string[] hashes, int maxHashes, CancellationToken ct);

    /// <summary>
    /// Streams all torrent entries from the database as an async sequence.
    /// </summary>
    /// <param name="ct">Token to cancel the enumeration.</param>
    /// <returns>An async enumerable of <see cref="StreamedEntry"/> entries.</returns>
    IAsyncEnumerable<StreamedEntry> StreamAllAsync(CancellationToken ct);

    /// <summary>
    /// Retrieves a single torrent by its info hash.
    /// </summary>
    /// <param name="infoHash">The info hash to look up.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>The matching <see cref="TorrentInfo"/>, or <c>null</c> if not found.</returns>
    Task<TorrentInfo?> GetByInfoHashAsync(string infoHash, CancellationToken ct);
}