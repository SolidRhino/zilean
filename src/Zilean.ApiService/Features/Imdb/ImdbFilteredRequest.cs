namespace Zilean.ApiService.Features.Imdb;

/// <summary>
/// Request model for the filtered IMDb search endpoint.
/// </summary>
public class ImdbFilteredRequest
{
    /// <summary>
    /// The search query text.
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// The release year to filter by.
    /// </summary>
    public int? Year { get; init; }

    /// <summary>
    /// The category to filter by (e.g. <c>movie</c>, <c>tvSeries</c>).
    /// </summary>
    public string? Category { get; init; }
}