namespace Zilean.Shared.Features.Dmm;

/// <summary>
/// Provides extension methods for <see cref="TorrentInfo"/>.
/// </summary>
public static class TorrentInfoExtensions
{
    /// <summary>
    /// Generates a cache key combining the parsed title, category, and year.
    /// </summary>
    /// <param name="torrentInfo">The torrent info instance to derive the key from.</param>
    /// <returns>A string key suitable for caching lookups.</returns>
    public static string CacheKey(this TorrentInfo torrentInfo) =>
        $"{torrentInfo.ParsedTitle}-{torrentInfo.Category}-{torrentInfo.Year}";
}
