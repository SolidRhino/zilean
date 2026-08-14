namespace Zilean.Database.Dtos;

/// <summary>
/// Defines the Lucene document field names used when indexing IMDb entries.
/// </summary>
public static class LuceneIndexEntry
{
    /// <summary>
    /// The field name storing the IMDb identifier.
    /// </summary>
    public const string ImdbId = "imdbId";
    /// <summary>
    /// The field name storing the entry title.
    /// </summary>
    public const string Title = "title";
    /// <summary>
    /// The field name storing the release year.
    /// </summary>
    public const string Year = "year";
    /// <summary>
    /// The field name storing the category (movie or TV).
    /// </summary>
    public const string Category = "category";
}
