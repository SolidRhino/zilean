namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Configuration for IMDb metadata import and torrent-to-IMDb-ID matching.
/// Bound from the <c>Zilean__Imdb</c> env var section.
/// </summary>
public class ImdbConfiguration
{
    /// <summary>
    /// Enables importing IMDb metadata and matching torrents to IMDb IDs.
    /// Set via the <c>Zilean__Imdb__EnableImportMatching</c> env var.
    /// </summary>
    public bool EnableImportMatching { get; set; } = true;

    /// <summary>
    /// Enables the IMDb metadata query endpoint.
    /// Set via the <c>Zilean__Imdb__EnableEndpoint</c> env var.
    /// </summary>
    public bool EnableEndpoint { get; set; } = true;

    /// <summary>
    /// Minimum fuzzy-match score (0–1) required to accept an IMDb ID match.
    /// Set via the <c>Zilean__Imdb__MinimumScoreMatch</c> env var.
    /// </summary>
    public double MinimumScoreMatch { get; set; } = 0.85;

    /// <summary>
    /// When true, uses all available CPU cores for IMDb matching.
    /// Set via the <c>Zilean__Imdb__UseAllCores</c> env var.
    /// </summary>
    public bool UseAllCores { get; set; } = false;

    /// <summary>
    /// Number of CPU cores to use for IMDb matching when <see cref="UseAllCores"/> is false.
    /// Set via the <c>Zilean__Imdb__NumberOfCores</c> env var.
    /// </summary>
    public int NumberOfCores { get; set; } = 2;

    /// <summary>
    /// When true, uses Lucene.NET as the fuzzy-matching engine instead of the default.
    /// Set via the <c>Zilean__Imdb__UseLucene</c> env var.
    /// </summary>
    public bool UseLucene { get; set; } = false;

    /// <summary>
    /// Maximum number of (title, year, category) → IMDb ID entries kept in the in-memory match cache.
    /// Each entry costs ~250-350 bytes; the default of 100,000 is ~25-50 MB. Reduce for memory-constrained hosts.
    /// </summary>
    public int MatchCacheSize { get; set; } = 100_000;

    /// <summary>
    /// Hours after which a long-running scraper logs a one-shot warning recommending a restart so the
    /// in-memory IMDb snapshot is rebuilt from the (potentially refreshed) ImdbFiles table.
    /// Set to 0 to disable the warning.
    /// </summary>
    public int SnapshotMaxAgeHours { get; set; } = 24;
}