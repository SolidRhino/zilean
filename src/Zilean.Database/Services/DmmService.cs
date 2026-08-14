namespace Zilean.Database.Services;

/// <summary>
/// Provides operations for tracking DMM ingestion metadata and parsed pages via EF Core.
/// </summary>
public class DmmService(IDbContextFactory<ZileanDbContext> dbContextFactory)
{
    /// <summary>
    /// Retrieves the DMM last-import metadata, if any.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The last import metadata, or <c>null</c> if no import has been recorded.</returns>
    public async Task<DmmLastImport?> GetDmmLastImportAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var dmmLastImport = await dbContext.ImportMetadata.AsNoTracking().FirstOrDefaultAsync(x => x.Key == MetadataKeys.DmmLastImport, cancellationToken: cancellationToken);

        return dmmLastImport?.Value.Deserialize<DmmLastImport>();
    }

    /// <summary>
    /// Persists the DMM last-import metadata, creating or updating the entry.
    /// </summary>
    /// <param name="dmmLastImport">The import metadata to store.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SetDmmImportAsync(DmmLastImport dmmLastImport)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var metadata = await dbContext.ImportMetadata.FirstOrDefaultAsync(x => x.Key == MetadataKeys.DmmLastImport);

        if (metadata is null)
        {
            metadata = new ImportMetadata
            {
                Key = MetadataKeys.DmmLastImport,
                Value = JsonSerializer.SerializeToDocument(dmmLastImport),
            };
            await dbContext.ImportMetadata.AddAsync(metadata);
            await dbContext.SaveChangesAsync();
            return;
        }

        metadata.Value = JsonSerializer.SerializeToDocument(dmmLastImport);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Records multiple parsed DMM pages as ingested.
    /// </summary>
    /// <param name="pageNames">The parsed pages to record.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddPagesToIngestedAsync(IEnumerable<ParsedPages> pageNames, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.ParsedPages.AddRangeAsync(pageNames, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Records a single parsed DMM page as ingested.
    /// </summary>
    /// <param name="pageNames">The parsed page to record.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddPageToIngestedAsync(ParsedPages pageNames, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.ParsedPages.AddAsync(pageNames, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves all previously parsed (ingested) DMM pages.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of <see cref="ParsedPages"/> entries.</returns>
    public async Task<List<ParsedPages>> GetIngestedPagesAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.ParsedPages.AsNoTracking().ToListAsync(cancellationToken: cancellationToken);
    }
}
