namespace Zilean.Scraper.Features.Imdb;

/// <summary>
/// Provides extension methods for <see cref="ImdbFile"/> in the ingestion context.
/// </summary>
public static class ImdbFileExtensions
{
    /// <summary>
    /// Retrieves IMDb file candidates whose year falls within ±1 of the specified year.
    /// </summary>
    /// <param name="imdbFiles">The year-keyed dictionary of IMDb files.</param>
    /// <param name="year">The target year to search around.</param>
    /// <returns>A list of candidate <see cref="ImdbFile"/> entries for the year range.</returns>
    public static List<ImdbFile> GetCandidatesForYearRange(this ConcurrentDictionary<int, List<ImdbFile>> imdbFiles, int year)
    {
        var candidates = new List<ImdbFile>();

        for (int y = year - 1; y <= year + 1; y++)
        {
            if (imdbFiles.TryGetValue(y, out var files))
            {
                candidates.AddRange(files);
            }
        }

        return candidates;
    }
}
