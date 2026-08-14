namespace Zilean.Database.Services;

/// <summary>
/// Extends <see cref="TorrentInfo"/> with aliased IMDb columns returned by <c>search_torrents_meta</c>.
/// </summary>
public class TorrentInfoResult : TorrentInfo
{
    // Aliased columns
    /// <summary>Gets the IMDb category alias from the search SQL (e.g. movie, tvSeries).</summary>
    public string? ImdbCategory { get; set; } // Matches the alias in SQL
    /// <summary>Gets the IMDb title alias from the search SQL.</summary>
    public string? ImdbTitle { get; set; }    // Matches the alias in SQL
    /// <summary>Gets the IMDb release year alias from the search SQL.</summary>
    public int? ImdbYear { get; set; }        // Matches the alias in SQL
    /// <summary>Gets a value indicating whether the IMDb entry is adult content.</summary>
    public bool ImdbAdult { get; set; }       // Matches the alias in SQL
}
