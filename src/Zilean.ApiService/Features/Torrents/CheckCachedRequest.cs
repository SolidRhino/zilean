namespace Zilean.ApiService.Features.Torrents;

/// <summary>
/// Request model for the cache-check endpoint.
/// </summary>
public class CheckCachedRequest
{
    /// <summary>
    /// A comma-separated list of torrent infohashes to check against the cache.
    /// </summary>
    public string? Hashes { get; set; }
}