namespace Zilean.Shared.Features.Torznab;

/// <summary>
/// Provides static Torznab capabilities metadata and generates the caps response XML
/// advertised to *arr clients via the <c>t=caps</c> query type.
/// </summary>
public static class TorznabCapabilities
{
    /// <summary>
    /// Supported TV search parameters advertised in the caps response.
    /// </summary>
    public static List<TvSearch> TvSearchParams { get; } =
    [
        TvSearch.Q,
        TvSearch.Season,
        TvSearch.Ep,
        TvSearch.ImdbId,
        TvSearch.Year,
    ];

    /// <summary>
    /// Supported movie search parameters advertised in the caps response.
    /// </summary>
    public static List<MovieSearch> MovieSearchParams { get; } =
    [
        MovieSearch.Q,
        MovieSearch.ImdbId,
        MovieSearch.Year,
    ];

    /// <summary>
    /// Supported XXX search parameters advertised in the caps response.
    /// </summary>
    public static List<XxxSearch> XxxSearchParams { get; } =
    [
        XxxSearch.Q,
        XxxSearch.Year,
        XxxSearch.ImdbId
    ];

    /// <summary>
    /// Supported book search parameters advertised in the caps response.
    /// </summary>
    public static List<BookSearch> BookSearchParams { get; } =
    [
        BookSearch.Q,
    ];

    /// <summary>
    /// The maximum number of results the indexer will return in a single page.
    /// </summary>
    public static int LimitsMax { get; } = 5000;
    /// <summary>
    /// The default page size when the client does not specify a limit.
    /// </summary>
    public static int LimitsDefault { get; } = 100;
    /// <summary>
    /// Whether generic keyword search is available.
    /// </summary>
    public static bool SearchAvailable { get; } = true;
    /// <summary>
    /// Whether the indexer supports raw search-engine queries.
    /// </summary>
    public static bool SupportsRawSearch { get; } = false;
    /// <summary>
    /// Whether TV search is available (at least one TV search parameter is supported).
    /// </summary>
    public static bool TvSearchAvailable => TvSearchParams.Count > 0;
    /// <summary>
    /// Whether the <c>season</c> parameter is supported for TV search.
    /// </summary>
    public static bool TvSearchSeasonAvailable => TvSearchParams.Contains(TvSearch.Season);
    /// <summary>
    /// Whether the <c>ep</c> parameter is supported for TV search.
    /// </summary>
    public static bool TvSearchEpAvailable => TvSearchParams.Contains(TvSearch.Ep);
    /// <summary>
    /// Whether the <c>imdbid</c> parameter is supported for TV search.
    /// </summary>
    public static bool TvSearchImdbAvailable => TvSearchParams.Contains(TvSearch.ImdbId);
    /// <summary>
    /// Whether the <c>year</c> parameter is supported for TV search.
    /// </summary>
    public static bool TvSearchYearAvailable => TvSearchParams.Contains(TvSearch.Year);
    /// <summary>
    /// Whether movie search is available (at least one movie search parameter is supported).
    /// </summary>
    public static bool MovieSearchAvailable => MovieSearchParams.Count > 0;
    /// <summary>
    /// Whether the <c>imdbid</c> parameter is supported for movie search.
    /// </summary>
    public static bool MovieSearchImdbAvailable => MovieSearchParams.Contains(MovieSearch.ImdbId);
    /// <summary>
    /// Whether the <c>year</c> parameter is supported for movie search.
    /// </summary>
    public static bool MovieSearchYearAvailable => MovieSearchParams.Contains(MovieSearch.Year);
    /// <summary>
    /// Whether XXX search is available (at least one XXX search parameter is supported).
    /// </summary>
    public static bool XxxSearchAvailable => XxxSearchParams.Count > 0;
    /// <summary>
    /// Whether the <c>imdbid</c> parameter is supported for XXX search.
    /// </summary>
    public static bool XxxSearchImdbAvailable => XxxSearchParams.Contains(XxxSearch.ImdbId);
    /// <summary>
    /// Whether the <c>year</c> parameter is supported for XXX search.
    /// </summary>
    public static bool XxxSearchYearAvailable => XxxSearchParams.Contains(XxxSearch.Year);
    /// <summary>
    /// Whether book search is available (at least one book search parameter is supported).
    /// </summary>
    public static bool BookSearchAvailable => BookSearchParams.Count > 0;

    /// <summary>
    /// The Torznab categories advertised in the caps response.
    /// </summary>
    public static List<TorznabCategory> Categories { get; } =
    [
        TorznabCategoryTypes.Movies,
        TorznabCategoryTypes.Audio,
        TorznabCategoryTypes.TV,
        TorznabCategoryTypes.XXX,
        TorznabCategoryTypes.Books,
    ];

    /// <summary>
    /// Serializes the capabilities metadata to a Torznab caps XML response string.
    /// </summary>
    /// <returns>The caps XML document as a string.</returns>
    public static string ToXml() =>
        GetXDocument().Declaration + Environment.NewLine + GetXDocument();

    private static XDocument GetXDocument()
    {
        var xdoc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("caps",
                new XElement("server",
                    new XAttribute("title", "Zilean")
                ),
                new XElement("limits",
                    new XAttribute("default", LimitsDefault),
                    new XAttribute("max", LimitsMax)
                ),
                new XElement("searching",
                    new XElement("search",
                        new XAttribute("available", SearchAvailable ? "yes" : "no"),
                        new XAttribute("supportedParams", "q"),
                        SupportsRawSearch ? new XAttribute("searchEngine", "raw") : null
                    ),
                    new XElement("tv-search",
                        new XAttribute("available", TvSearchAvailable ? "yes" : "no"),
                        new XAttribute("supportedParams", SupportedTvSearchParams()),
                        SupportsRawSearch ? new XAttribute("searchEngine", "raw") : null
                    ),
                    new XElement("movie-search",
                        new XAttribute("available", MovieSearchAvailable ? "yes" : "no"),
                        new XAttribute("supportedParams", SupportedMovieSearchParams()),
                        SupportsRawSearch ? new XAttribute("searchEngine", "raw") : null
                    ),
                    new XElement("xxx-search",
                        new XAttribute("available", XxxSearchAvailable ? "yes" : "no"),
                        new XAttribute("supportedParams", SupportedXxxSearchParams()),
                        SupportsRawSearch ? new XAttribute("searchEngine", "raw") : null
                    )
                    ,new XElement("book-search",
                        new XAttribute("available", BookSearchAvailable ? "yes" : "no"),
                        new XAttribute("supportedParams", SupportedBookSearchParams()),
                        SupportsRawSearch ? new XAttribute("searchEngine", "raw") : null
                    )
                ),
                new XElement("categories",
                    from c in Categories.GetTorznabCategoryTree()
                    select new XElement("category",
                        new XAttribute("id", c.Id),
                        new XAttribute("name", c.Name),
                        from sc in c.SubCategories
                        select new XElement("subcat",
                            new XAttribute("id", sc.Id),
                            new XAttribute("name", sc.Name)
                        )
                    )
                )
            )
        );
        return xdoc;
    }

    private static string SupportedTvSearchParams()
    {
        var parameters = new List<string> { "q" };

        if (TvSearchSeasonAvailable)
        {
            parameters.Add("season");
        }

        if (TvSearchEpAvailable)
        {
            parameters.Add("ep");
        }

        if (TvSearchImdbAvailable)
        {
            parameters.Add("imdbid");
        }

        if (TvSearchYearAvailable)
        {
            parameters.Add("year");
        }

        return string.Join(",", parameters);
    }

    private static string SupportedMovieSearchParams()
    {
        var parameters = new List<string> { "q" };

        if (MovieSearchImdbAvailable)
        {
            parameters.Add("imdbid");
        }

        if (MovieSearchYearAvailable)
        {
            parameters.Add("year");
        }

        return string.Join(",", parameters);
    }

    private static string SupportedXxxSearchParams()
    {
        var parameters = new List<string> { "q" };

        if (XxxSearchImdbAvailable)
        {
            parameters.Add("imdbid");
        }

        if (XxxSearchYearAvailable)
        {
            parameters.Add("year");
        }

        return string.Join(",", parameters);
    }

    private static string SupportedBookSearchParams()
    {
        var parameters = new List<string> { "q" };

        return string.Join(",", parameters);
    }
}
