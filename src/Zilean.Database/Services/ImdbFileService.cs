namespace Zilean.Database.Services;

/// <summary>
/// Default implementation of <see cref="IImdbFileService"/> for staging, bulk-storing,
/// and searching IMDb metadata using EF Core.
/// </summary>
public class ImdbFileService(ILogger<ImdbFileService> logger, IDbContextFactory<ZileanDbContext> dbContextFactory)
    : IImdbFileService
{
    private ConcurrentBag<ImdbFile> ImdbFiles { get; } = [];
    /// <inheritdoc/>
    public void AddImdbFile(ImdbFile imdbFile) => ImdbFiles.Add(imdbFile);
    /// <inheritdoc/>
    public async Task StoreImdbFiles()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        if (ImdbFiles.IsEmpty)
        {
            logger.LogInformation("No imdb files to store.");
            return;
        }

        var bulkConfig = new BulkConfig
        {
            SetOutputIdentity = false,
            BatchSize = 5000,
            PropertiesToIncludeOnUpdate = [string.Empty],
            UpdateByProperties = ["ImdbId"],
            BulkCopyTimeout = 0,
            TrackingEntities = false,
        };

        dbContext.Database.SetCommandTimeout(0);

        logger.LogInformation("Storing {Count} imdb entries", ImdbFiles.Count);

        await dbContext.BulkInsertOrUpdateAsync(ImdbFiles, bulkConfig);

        var imdbLastImport = new ImdbLastImport
        {
            OccuredAt = DateTime.UtcNow,
            EntryCount = ImdbFiles.Count,
            Status = ImportStatus.Complete
        };

        await SetImdbLastImportAsync(imdbLastImport);
    }

    /// <inheritdoc/>
    public async Task VaccumImdbFilesIndexes(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.ExecuteSqlRawAsync("VACUUM (VERBOSE, ANALYZE) \"ImdbFiles\"", cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ImdbSearchResult[]> SearchForImdbIdAsync(string query, int? year = null, string? category = null)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        const string sql =
            """
            SELECT
                imdb_id as "ImdbId",
                title as "Title",
                year as "Year",
                score as "Score",
                category as "Category"
            FROM search_imdb_meta(@query, @category, @year, 10)
            """;

        var parameters = new object[]
        {
            new NpgsqlParameter("@query", (object?)query ?? DBNull.Value),
            new NpgsqlParameter("@category", (object?)category ?? DBNull.Value),
            new NpgsqlParameter("@year", (object?)year ?? DBNull.Value),
        };

        var results = await dbContext.Database.SqlQueryRaw<ImdbSearchResult>(sql, parameters).ToArrayAsync();

        return results;
    }

    /// <inheritdoc/>
    public async Task<ImdbLastImport?> GetImdbLastImportAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var imdbLastImport = await dbContext.ImportMetadata.AsNoTracking().FirstOrDefaultAsync(x => x.Key == MetadataKeys.ImdbLastImport, cancellationToken: cancellationToken);

        return imdbLastImport?.Value.Deserialize<ImdbLastImport>();
    }

    /// <inheritdoc/>
    public async Task SetImdbLastImportAsync(ImdbLastImport imdbLastImport)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var metadata = await dbContext.ImportMetadata.FirstOrDefaultAsync(x => x.Key == MetadataKeys.ImdbLastImport);

        if (metadata is null)
        {
            metadata = new ImportMetadata
            {
                Key = MetadataKeys.ImdbLastImport,
                Value = JsonSerializer.SerializeToDocument(imdbLastImport),
            };
            await dbContext.ImportMetadata.AddAsync(metadata);
            await dbContext.SaveChangesAsync();
            return;
        }

        metadata.Value = JsonSerializer.SerializeToDocument(imdbLastImport);
        await dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public int ImdbFileCount => ImdbFiles.Count;
}
