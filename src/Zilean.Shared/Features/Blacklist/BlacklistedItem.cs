namespace Zilean.Shared.Features.Blacklist;

/// <summary>
/// Represents a blacklisted torrent item that should be excluded from search results.
/// </summary>
public class BlacklistedItem
{
    /// <summary>
    /// The BitTorrent infohash of the blacklisted torrent.
    /// </summary>
    [JsonPropertyName("info_hash")]
    public string? InfoHash { get; set; }

    /// <summary>
    /// The reason the torrent was blacklisted.
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// The UTC timestamp when the torrent was blacklisted.
    /// </summary>
    [JsonPropertyName("blacklisted_at")]
    public DateTime? BlacklistedAt { get; set; }
}