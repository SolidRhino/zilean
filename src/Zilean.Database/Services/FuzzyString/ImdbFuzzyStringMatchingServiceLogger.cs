namespace Zilean.Database.Services.FuzzyString;

/// <summary>
/// Source-generated logger helper methods for <see cref="ImdbFuzzyStringMatchingService"/>.
/// </summary>
public static partial class ImdbFuzzyStringMatchingServiceLogger
{
    /// <summary>
    /// Logs that no suitable IMDb match was found for the given torrent.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="title">The torrent title that could not be matched.</param>
    /// <param name="category">The torrent category (movie or TV).</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "No suitable match found for Torrent '{Title}', Category: {Category}")]
    public static partial void NoSuitableMatchFound(this ILogger logger, string title, string category);

    /// <summary>
    /// Logs that a torrent's IMDb ID was updated to a new match.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="title">The torrent title that was matched.</param>
    /// <param name="oldImdbId">The previous IMDb identifier, if any.</param>
    /// <param name="newImdbId">The newly assigned IMDb identifier.</param>
    /// <param name="score">The match confidence score.</param>
    /// <param name="category">The torrent category (movie or TV).</param>
    /// <param name="imdbTitle">The matched IMDb entry title.</param>
    /// <param name="imdbYear">The matched IMDb entry year.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Torrent '{Title}' updated from IMDb ID '{OldImdbId}' to '{NewImdbId}' with a score of {Score}, Category: {Category}, Imdb Title: {ImdbTitle}, Imdb Year: {ImdbYear}")]
    public static partial void TorrentUpdated(
        this ILogger logger,
        string title,
        string oldImdbId,
        string newImdbId,
        double score,
        string category,
        string imdbTitle,
        int imdbYear);

    /// <summary>
    /// Logs that a torrent retained its existing IMDb ID because no better match was found.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="title">The torrent title that was matched.</param>
    /// <param name="imdbId">The retained IMDb identifier.</param>
    /// <param name="score">The best match confidence score.</param>
    /// <param name="category">The torrent category (movie or TV).</param>
    /// <param name="imdbTitle">The matched IMDb entry title.</param>
    /// <param name="imdbYear">The matched IMDb entry year.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Torrent '{Title}' retained its existing IMDb ID '{ImdbId}' with a best match score of {Score}, Category: {Category}, Imdb Title: {ImdbTitle}, Imdb Year: {ImdbYear}")]
    public static partial void TorrentRetained(
        this ILogger logger,
        string title,
        string imdbId,
        double score,
        string category,
        string imdbTitle,
        int imdbYear);
}
