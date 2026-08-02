namespace Zilean.Database.Services;

public class TorrentInfoService(ILogger<TorrentInfoService> logger, ZileanConfiguration configuration, IDbContextFactory<ZileanDbContext> dbContextFactory, IImdbMatchingService imdbMatchingService)
    : ITorrentInfoService
{
    public async Task VaccumTorrentsIndexes(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync("VACUUM (VERBOSE, ANALYZE) \"Torrents\"", cancellationToken: cancellationToken);
    }

    public async Task<StoreResult> StoreTorrentInfo(List<TorrentInfo> torrents, int batchSize = 5000)
    {
        if (torrents.Count == 0)
        {
            logger.LogInformation("No torrents to store.");
            return new StoreResult(Stored: 0, PopulateMs: 0, MatchMs: 0, UpsertMs: 0);
        }

        foreach (var torrentInfo in torrents)
        {
            torrentInfo.CleanedParsedTitle = Parsing.CleanQuery(torrentInfo.ParsedTitle);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        long populateMs = 0;
        long matchMs = 0;
        long upsertMs = 0;

        // PopulateImdbData is idempotent and the matcher is a singleton,
        // so this is a no-op after the first ingestion batch in the process.
        // Don't dispose at the end of the method - keep the in-memory state hot
        // for subsequent batches. ResyncImdbCommand handles its own lifecycle
        // when refreshing IMDb data.
        if (configuration.Imdb.EnableImportMatching)
        {
            var populateStart = Stopwatch.GetTimestamp();
            await imdbMatchingService.PopulateImdbData();
            populateMs = (long)Stopwatch.GetElapsedTime(populateStart).TotalMilliseconds;
        }

        var bulkConfig = new BulkConfig
        {
            SetOutputIdentity = false,
            BatchSize = batchSize,
            PropertiesToIncludeOnUpdate = [string.Empty],
            UpdateByProperties = ["InfoHash"],
            BulkCopyTimeout = 0,
            TrackingEntities = false,
        };

        dbContext.Database.SetCommandTimeout(0);

        var chunks = torrents.Chunk(batchSize).ToList();

        logger.LogInformation("Storing {Count} torrents in {BatchSize} batches", torrents.Count, chunks.Count);
        var currentBatch = 0;
        foreach (var batch in chunks)
        {
            currentBatch++;

            if (configuration.Imdb.EnableImportMatching)
            {
                logger.LogInformation("Matching IMDb IDs for batch {CurrentBatch} of {TotalBatches}", currentBatch, chunks.Count);
                var matchStart = Stopwatch.GetTimestamp();
                await imdbMatchingService.MatchImdbIdsForBatchAsync(batch);
                matchMs += (long)Stopwatch.GetElapsedTime(matchStart).TotalMilliseconds;
            }

            logger.LogInformation("Storing batch {CurrentBatch} of {TotalBatches}", currentBatch, chunks.Count);
            var upsertStart = Stopwatch.GetTimestamp();
            await dbContext.BulkInsertOrUpdateAsync(batch, bulkConfig);
            upsertMs += (long)Stopwatch.GetElapsedTime(upsertStart).TotalMilliseconds;
        }

        return new StoreResult(Stored: torrents.Count, PopulateMs: populateMs, MatchMs: matchMs, UpsertMs: upsertMs);
    }

    public async Task<TorrentInfo[]> SearchForTorrentInfoByOnlyTitle(string query)
    {
        var cleanQuery = Parsing.CleanQuery(query);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var sql =
            """
            SELECT *
            FROM "Torrents"
            WHERE "ParsedTitle" % @query
            AND Length("InfoHash") = 40
            LIMIT 100
            """;

        var results = await dbContext.Torrents.FromSqlRaw(sql, new NpgsqlParameter("@query", cleanQuery)).ToArrayAsync();

        return results;
    }

    public async Task<TorrentInfo[]> SearchForTorrentInfoFiltered(TorrentInfoFilter filter, int? limit = null)
    {
        var (queryWithoutYear, extractedYear) = Parsing.ExtractTrailingYear(filter.Query);
        var cleanQuery = Parsing.CleanQuery(queryWithoutYear);
        var effectiveYear = filter.Year ?? extractedYear;
        var imdbId = EnsureCorrectFormatImdbId(filter);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        const string sql =
            """
            SELECT *
            FROM search_torrents_meta(
                @Query,
                @Season,
                @Episode,
                @Year,
                @Language,
                @Resolution,
                @ImdbId,
                @Limit,
                @Category,
                @SimilarityThreshold
            )
            """;

        var parameters = new object[]
        {
            new NpgsqlParameter("@Query", (object?)cleanQuery ?? DBNull.Value),
            new NpgsqlParameter("@Season", (object?)filter.Season ?? DBNull.Value),
            new NpgsqlParameter("@Episode", (object?)filter.Episode ?? DBNull.Value),
            new NpgsqlParameter("@Year", (object?)effectiveYear ?? DBNull.Value),
            new NpgsqlParameter("@Language", (object?)filter.Language ?? DBNull.Value),
            new NpgsqlParameter("@Resolution", (object?)filter.Resolution ?? DBNull.Value),
            new NpgsqlParameter("@ImdbId", (object?)imdbId ?? DBNull.Value),
            new NpgsqlParameter("@Limit", (object?)(limit ?? configuration.Dmm.MaxFilteredResults)),
            new NpgsqlParameter("@Category", (object?)filter.Category ?? DBNull.Value),
            new NpgsqlParameter("@SimilarityThreshold", (float)configuration.Dmm.MinimumScoreMatch),
        };

        var rows = await dbContext.Database.SqlQueryRaw<TorrentInfoQueryDto>(sql, parameters).ToArrayAsync();

        return rows.Select(dto => MapImdbDataToTorrentInfo(dto.ToTorrentInfoResult())).ToArray();
    }

    private static string? EnsureCorrectFormatImdbId(TorrentInfoFilter filter)
    {
        string? imdbId = null;
        if (!string.IsNullOrEmpty(filter.ImdbId))
        {
            imdbId = filter.ImdbId.StartsWith("tt") ? filter.ImdbId : $"tt{filter.ImdbId}";
        }

        return imdbId;
    }

    private static Func<TorrentInfoResult, TorrentInfoResult> MapImdbDataToTorrentInfo =>
        torrentInfo =>
        {
            if (torrentInfo.ImdbId != null)
            {
                torrentInfo.Imdb = new()
                {
                    ImdbId = torrentInfo.ImdbId,
                    Category = torrentInfo.ImdbCategory,
                    Title = torrentInfo.ImdbTitle,
                    Year = torrentInfo.ImdbYear ?? 0,
                    Adult = torrentInfo.ImdbAdult,
                };
            }

            return torrentInfo;
        };

    public async Task<HashSet<string>> GetExistingInfoHashesAsync(List<string> infoHashes)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var existingHashes = await dbContext.Torrents
            .Where(t => infoHashes.Contains(t.InfoHash))
            .Select(t => t.InfoHash)
            .ToListAsync();

        return [..existingHashes];
    }

    public async Task<HashSet<string>> GetBlacklistedItems()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var existingHashes = await dbContext.BlacklistedItems
            .Select(t => t.InfoHash)
            .ToListAsync();

        return [..existingHashes];
    }
}
