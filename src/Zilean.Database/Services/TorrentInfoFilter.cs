namespace Zilean.Database.Services;

/// <summary>
/// Filter criteria used to narrow <see cref="ITorrentInfoService.SearchForTorrentInfoFiltered"/> queries.
/// </summary>
public class TorrentInfoFilter
{
    /// <summary>Gets or sets the free-text search query matched against torrent titles.</summary>
    public string? Query { get; init; }
    /// <summary>Gets or sets the season number to filter by.</summary>
    public int? Season { get; init; }
    /// <summary>Gets or sets the episode number to filter by.</summary>
    public int? Episode { get; init; }
    /// <summary>Gets or sets the release year to filter by.</summary>
    public int? Year { get; init; }
    /// <summary>Gets or sets the language to filter by.</summary>
    public string? Language { get; init; }
    /// <summary>Gets or sets the resolution to filter by (e.g. 1080p).</summary>
    public string? Resolution { get; init; }
    /// <summary>Gets or sets the IMDb identifier to filter by (e.g. tt1234567).</summary>
    public string? ImdbId { get; init; }
    /// <summary>Gets or sets the Torznab category identifier to filter by.</summary>
    public string? Category { get; set; }
}
