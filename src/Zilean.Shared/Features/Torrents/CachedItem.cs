namespace Zilean.Shared.Features.Torrents;

/// <summary>
/// Represents the cached status of a torrent item from a debrid provider.
/// </summary>
public class CachedItem
{
    /// <summary>
    /// The BitTorrent infohash identifying the torrent.
    /// </summary>
    [JsonPropertyName("info_hash")]
    public string? InfoHash { get; set; }

    /// <summary>
    /// Whether the torrent is currently cached on the debrid provider.
    /// </summary>
    [JsonPropertyName("is_cached")]
    public bool? IsCached { get; set; }

    /// <summary>
    /// The parsed torrent metadata associated with this cached item, if available.
    /// </summary>
    [JsonPropertyName("item")]
    public TorrentInfo? Item { get; set; }
}