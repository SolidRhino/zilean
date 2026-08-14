namespace Zilean.Database.Dtos;

/// <summary>
/// Represents a single IMDb search result from the Lucene or fuzzy-string matching pipeline.
/// </summary>
public class ImdbSearchResult
{
    /// <summary>
    /// Gets or sets the matched IMDb entry title.
    /// </summary>
    public string? Title { get; set; }
    /// <summary>
    /// Gets or sets the matched IMDb identifier (e.g. <c>tt1234567</c>).
    /// </summary>
    public string? ImdbId { get; set; }
    /// <summary>
    /// Gets or sets the matched IMDb entry release year.
    /// </summary>
    public int Year { get; set; }
    /// <summary>
    /// Gets or sets the match confidence score (higher is better).
    /// </summary>
    public double Score { get; set; }
    /// <summary>
    /// Gets or sets the matched IMDb category (e.g. <c>movie</c> or <c>tv</c>).
    /// </summary>
    public string? Category { get; set; }
}
