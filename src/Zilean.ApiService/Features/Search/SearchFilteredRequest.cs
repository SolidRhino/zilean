namespace Zilean.ApiService.Features.Search;

/// <summary>
/// Request model for the filtered DMM search endpoint.
/// </summary>
public class SearchFilteredRequest
{
    /// <summary>
    /// The search query text.
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// The season number to filter by.
    /// </summary>
    public int? Season { get; init; }

    /// <summary>
    /// The episode number to filter by.
    /// </summary>
    public int? Episode { get; init; }

    /// <summary>
    /// The release year to filter by.
    /// </summary>
    public int? Year { get; init; }

    /// <summary>
    /// The language to filter by.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// The resolution to filter by (e.g. <c>1080p</c>).
    /// </summary>
    public string? Resolution { get; init; }

    /// <summary>
    /// The IMDb identifier to filter by.
    /// </summary>
    public string? ImdbId { get; init; }

    /// <summary>
    /// The category to filter by (e.g. <c>movie</c>, <c>tvSeries</c>).
    /// </summary>
    public string? Category { get; init; }
}