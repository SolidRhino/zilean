namespace Zilean.Shared.Features.Imdb;

/// <summary>
/// Represents an IMDb metadata entry used for matching torrents to movies and TV shows.
/// </summary>
public class ImdbFile
{
    /// <summary>
    /// The IMDb identifier (e.g. <c>tt1234567</c>).
    /// </summary>
    [Key]
    public string ImdbId { get; set; } = default!;

    /// <summary>
    /// The IMDb category (e.g. <c>movie</c> or <c>tvSeries</c>).
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// The title of the movie or TV show.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Whether the entry is classified as adult content.
    /// </summary>
    public bool Adult { get; set; }

    /// <summary>
    /// The release year of the movie or TV show.
    /// </summary>
    public int Year { get; set; }
}