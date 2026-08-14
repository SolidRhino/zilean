namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Configuration for torrent hash lookup endpoints.
/// Bound from the <c>Zilean__Torrents</c> env var section.
/// </summary>
public class TorrentsConfiguration
{
    /// <summary>
    /// Enables the torrent metadata query endpoint.
    /// Set via the <c>Zilean__Torrents__EnableEndpoint</c> env var.
    /// </summary>
    public bool EnableEndpoint { get; set; } = false;

    /// <summary>
    /// Maximum number of info-hashes accepted per batch lookup request.
    /// Set via the <c>Zilean__Torrents__MaxHashesToCheck</c> env var.
    /// </summary>
    public int MaxHashesToCheck { get; set; } = 100;

    /// <summary>
    /// Enables the scrape endpoint that reports torrent seeder/leecher counts.
    /// Set via the <c>Zilean__Torrents__EnableScrapeEndpoint</c> env var.
    /// </summary>
    public bool EnableScrapeEndpoint { get; set; } = false;

    /// <summary>
    /// Enables the cache-check endpoint for verifying whether info-hashes are already known.
    /// Set via the <c>Zilean__Torrents__EnableCacheCheckEndpoint</c> env var.
    /// </summary>
    public bool EnableCacheCheckEndpoint { get; set; } = false;
}