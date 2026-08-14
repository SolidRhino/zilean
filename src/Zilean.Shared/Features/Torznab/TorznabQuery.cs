namespace Zilean.Shared.Features.Torznab;

/// <summary>
/// Models a Torznab search query received from *arr clients (Prowlarr/Sonarr/Radarr).
/// </summary>
public partial class TorznabQuery
{
    [GeneratedRegex(@"\p{Pd}+", RegexOptions.Compiled)]
    private static partial Regex StandardizeDashesRegex();

    [GeneratedRegex(@"[\u0060\u00B4\u2018\u2019]", RegexOptions.Compiled)]
    private static partial Regex StandardizeSingleQuotesRegex();
    [GeneratedRegex("[^\\w]+")]
    private static partial Regex SplitRegex();
    /// <summary>
    /// Whether this is an interactive (manual) search rather than an automatic one.
    /// </summary>
    public bool InteractiveSearch { get; set; }
    /// <summary>
    /// The Torznab <c>t</c> parameter identifying the query type (e.g. <c>search</c>, <c>tvsearch</c>, <c>movie</c>).
    /// </summary>
    public string? QueryType { get; set; }
    /// <summary>
    /// The requested Torznab category IDs.
    /// </summary>
    public int[] Categories { get; set; } = [];
    /// <summary>
    /// The Torznab <c>extended</c> attribute flag controlling whether extended metadata is returned.
    /// </summary>
    public int Extended { get; set; }
    /// <summary>
    /// The maximum number of results to return.
    /// </summary>
    public int Limit { get; set; }
    /// <summary>
    /// The zero-based offset into the result set for pagination.
    /// </summary>
    public int Offset { get; set; }
    /// <summary>
    /// The IMDb ID (<c>tt1234567</c>) if the query targets a specific title.
    /// </summary>
    public string? ImdbID { get; set; }
    /// <summary>
    /// The search term split into individual query-string parts used for matching.
    /// </summary>
    public string[]? QueryStringParts { get; set; }
    /// <summary>
    /// The season number for TV searches, if specified.
    /// </summary>
    public int? Season { get; set; }
    /// <summary>
    /// The episode number for TV searches, if specified.
    /// </summary>
    public int? Episode { get; set; }
    /// <summary>
    /// The raw search term provided by the client.
    /// </summary>
    public string? SearchTerm { get; set; }
    /// <summary>
    /// The release year filter, if specified.
    /// </summary>
    public int? Year { get; set; }
    /// <summary>
    /// Whether this is a test query that should not count against rate limits.
    /// </summary>
    public bool IsTest { get; set; } = false;

    /// <summary>
    /// The IMDb ID with leading <c>t</c> characters stripped (numeric portion only).
    /// </summary>
    public string ImdbIDShort => ImdbID?.TrimStart('t');

    /// <summary>
    /// Whether the query type is a generic keyword search.
    /// </summary>
    public bool IsSearch => QueryType == "search";

    /// <summary>
    /// Whether the query type is a TV search.
    /// </summary>
    public bool IsTVSearch => QueryType == "tvsearch";
    /// <summary>
    /// Whether the query type is an adult (XXX) search.
    /// </summary>
    public bool IsXxxSearch => QueryType == "xxx";

    /// <summary>
    /// Whether the query type is a movie search.
    /// </summary>
    public bool IsMovieSearch => QueryType == "movie";
    /// <summary>
    /// Whether the query type is a book search.
    /// </summary>
    public bool IsBookSearch => QueryType == "book-search";
    /// <summary>
    /// Whether the query type is an audio search.
    /// </summary>
    public bool IsAudioSearch => QueryType == "audio-search";
    /// <summary>
    /// Whether the query includes an IMDb ID.
    /// </summary>
    public bool IsImdbQuery => ImdbID != null;

    /// <summary>
    /// Whether this is an RSS (backfill) search with no explicit search term or ID criteria.
    /// </summary>
    public bool IsRssSearch =>
        string.IsNullOrWhiteSpace(SearchTerm) &&
        !IsIdSearch;

    /// <summary>
    /// Whether the query targets a specific episode, season, IMDb ID, or year.
    /// </summary>
    public bool IsIdSearch =>
        Episode.GetValueOrDefault() > 0 ||
        Season.GetValueOrDefault() > 0 ||
        IsImdbQuery ||
        Year.GetValueOrDefault() > 0;

    /// <summary>
    /// Whether the client specified one or more category filters.
    /// </summary>
    public bool HasSpecifiedCategories => Categories is { Length: > 0 };

    /// <summary>
    /// The search term with dashes/quotes standardized and non-alphanumeric characters
    /// stripped for safe matching against release titles.
    /// </summary>
    public string SanitizedSearchTerm
    {
        get
        {
            var term = SearchTerm ?? "";

            term = StandardizeDashesRegex().Replace(term, "-");
            term = StandardizeSingleQuotesRegex().Replace(term, "'");

            var safeTitle = term.Where(c => char.IsLetterOrDigit(c)
                                             || char.IsWhiteSpace(c)
                                             || c == '-'
                                             || c == '.'
                                             || c == '_'
                                             || c == '('
                                             || c == ')'
                                             || c == '@'
                                             || c == '/'
                                             || c == '\''
                                             || c == '['
                                             || c == ']'
                                             || c == '+'
                                             || c == '%'
                                             || c == ':'
                                           );

            return string.Concat(safeTitle);
        }
    }

    /// <summary>
    /// Creates a clone of this query configured as a fallback movie search,
    /// defaulting to all movie subcategories if none were specified.
    /// </summary>
    /// <param name="search">The fallback search term to use.</param>
    /// <returns>A new <see cref="TorznabQuery"/> with movie categories and the given search term.</returns>
    public TorznabQuery CreateFallback(string? search)
    {
        var ret = Clone();
        if (Categories.Length == 0)
        {
            ret.Categories =
            [
                TorznabCategoryTypes.Movies.Id,
                TorznabCategoryTypes.MoviesForeign.Id,
                TorznabCategoryTypes.MoviesOther.Id,
                TorznabCategoryTypes.MoviesSD.Id,
                TorznabCategoryTypes.MoviesHD.Id,
                TorznabCategoryTypes.Movies3D.Id,
                TorznabCategoryTypes.MoviesBluRay.Id,
                TorznabCategoryTypes.MoviesDVD.Id,
                TorznabCategoryTypes.MoviesWEBDL.Id,
                TorznabCategoryTypes.MoviesUHD.Id
            ];
        }
        ret.SearchTerm = search;

        return ret;
    }

    /// <summary>
    /// Creates a deep copy of this query, including categories and query-string parts.
    /// </summary>
    /// <returns>A new <see cref="TorznabQuery"/> with all fields copied.</returns>
    public TorznabQuery Clone()
    {
        var ret = new TorznabQuery
        {
            InteractiveSearch = InteractiveSearch,
            QueryType = QueryType,
            Extended = Extended,
            Limit = Limit,
            Offset = Offset,
            Season = Season,
            Episode = Episode,
            SearchTerm = SearchTerm,
            IsTest = IsTest,
            Year = Year,
            ImdbID = ImdbID,
        };

        if (Categories.Length > 0)
        {
            ret.Categories = new int[Categories.Length];
            Array.Copy(Categories, ret.Categories, Categories.Length);
        }

        if (QueryStringParts?.Length > 0)
        {
            ret.QueryStringParts = new string[QueryStringParts.Length];
            Array.Copy(QueryStringParts, ret.QueryStringParts, QueryStringParts.Length);
        }

        return ret;
    }

    /// <summary>
    /// Returns the combined sanitized search term and episode search string, trimmed.
    /// </summary>
    /// <returns>The full query string used for matching.</returns>
    public string GetQueryString() => (SanitizedSearchTerm + " " + GetEpisodeSearchString()).Trim();

    /// <summary>
    /// Checks whether all query-string parts appear in the given title (AND semantics),
    /// ignoring common words and splitting on the first <paramref name="limit"/> characters.
    /// </summary>
    /// <param name="title">The release title to match against.</param>
    /// <param name="limit">Optional character limit applied to the query string before splitting.</param>
    /// <param name="queryStringOverride">Optional override for the query string;
    /// defaults to <see cref="GetQueryString"/>.</param>
    /// <returns><c>true</c> if every query-string part is found in <paramref name="title"/>; otherwise <c>false</c>.</returns>
    public bool MatchQueryStringAnd(string title, int? limit = null, string? queryStringOverride = null)
    {
        var commonWords = new[] { "and", "the", "an" };

        if (QueryStringParts == null)
        {
            var queryString = !string.IsNullOrWhiteSpace(queryStringOverride) ? queryStringOverride : GetQueryString();

            if (limit is > 0)
            {
                if (limit > queryString.Length)
                {
                    limit = queryString.Length;
                }

                queryString = queryString[..(int)limit];
            }

            QueryStringParts = SplitRegex().Split(queryString).Where(p => !string.IsNullOrWhiteSpace(p) && p.Length > 1 && !commonWords.ContainsIgnoreCase(p)).ToArray();
        }

        // Check if each part of the query string is in the given title.
        return QueryStringParts.All(title.ContainsIgnoreCase);
    }

    /// <summary>
    /// Builds the episode portion of the search string (e.g. <c>S01E05</c> or a date for daily shows).
    /// </summary>
    /// <returns>The episode search string, or <see cref="string.Empty"/> if no season is specified.</returns>
    public string GetEpisodeSearchString()
    {
        if (Season is null or 0)
        {
            return string.Empty;
        }

        string episodeString;
        if (DateTime.TryParseExact($"{Season} {Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
        {
            episodeString = showDate.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
        }
        else if (!Episode.HasValue)
        {
            episodeString = $"S{Season:00}";
        }
        else
        {
            try
            {
                episodeString = $"S{Season:00}E{Parsing.CoerceInt(Episode.GetValueOrDefault().ToString()):00}";
            }
            catch (FormatException) // e.g. seaching for S01E01A
            {
                episodeString = $"S{Season:00}E{Episode}";
            }
        }

        return episodeString;
    }
}
