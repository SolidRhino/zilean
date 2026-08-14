namespace Zilean.Shared.Features.Torznab.Parameters;

/// <summary>
/// Defines the supported parameters for Torznab TV search queries.
/// </summary>
public enum TvSearch
{
    /// <summary>
    /// The free-text search query parameter.
    /// </summary>
    Q,
    /// <summary>
    /// The season number parameter.
    /// </summary>
    Season,
    /// <summary>
    /// The episode number parameter.
    /// </summary>
    Ep,
    /// <summary>
    /// The IMDb ID parameter.
    /// </summary>
    ImdbId,
    /// <summary>
    /// The release year parameter.
    /// </summary>
    Year,
}
