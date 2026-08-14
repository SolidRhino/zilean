// ReSharper disable InconsistentNaming
namespace Zilean.ApiService.Features.Torznab;

/// <summary>
/// Models the query parameters sent by *arr clients to the Torznab API endpoint.
/// </summary>
public class TorznabRequest
{
    /// <summary>
    /// The free-text search query.
    /// </summary>
    public string? q { get; set; }

    /// <summary>
    /// The IMDb identifier to search for (e.g. <c>tt1234567</c>).
    /// </summary>
    public string? imdbid { get; set; }

    /// <summary>
    /// The episode number for TV searches.
    /// </summary>
    public string? ep { get; set; }

    /// <summary>
    /// The query type (<c>search</c>, <c>movie</c>, <c>tvsearch</c>, <c>caps</c>, etc.).
    /// </summary>
    public string? t { get; set; }

    /// <summary>
    /// Whether extended metadata attributes should be included in the response.
    /// </summary>
    public string? extended { get; set; }

    /// <summary>
    /// The maximum number of results to return.
    /// </summary>
    public string? limit { get; set; }

    /// <summary>
    /// The zero-based offset for paginated results.
    /// </summary>
    public string? offset { get; set; }

    /// <summary>
    /// The comma-separated Torznab category IDs to filter by.
    /// </summary>
    public string? cat { get; set; }

    /// <summary>
    /// The season number for TV searches.
    /// </summary>
    public string? season { get; set; }

    /// <summary>
    /// The release year to filter by.
    /// </summary>
    public string? year { get; set; }
}