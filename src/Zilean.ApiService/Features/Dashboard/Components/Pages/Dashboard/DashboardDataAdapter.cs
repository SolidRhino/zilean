namespace Zilean.ApiService.Features.Dashboard.Components.Pages.Dashboard;

/// <summary>
/// Syncfusion <c>DataAdaptor</c> that bridges the dashboard grid with the
/// <see cref="ZileanDbContext"/>, supporting read, insert, update, remove and batch
/// operations against <see cref="TorrentInfo"/> records.
/// </summary>
/// <param name="dbContextFactory">Factory for creating scoped <see cref="ZileanDbContext"/> instances.</param>
/// <param name="parseTorrentNameService">Service that re-parses torrent titles via the Python runtime.</param>
/// <param name="pythonRuntimeService">Service exposing Python runtime availability.</param>
/// <param name="logger">Logger for recording data operation failures.</param>
public class DashboardDataAdapter(IDbContextFactory<ZileanDbContext> dbContextFactory, TorrentParser parseTorrentNameService, PythonRuntimeService pythonRuntimeService, ILogger<DashboardDataAdapter> logger) : DataAdaptor
{
    /// <summary>
    /// Reads torrent records from the database applying the Syncfusion data manager
    /// search, filter, sort, skip and take parameters.
    /// </summary>
    /// <param name="dataManagerRequest">The Syncfusion data manager request containing search, filter, sort and paging options.</param>
    /// <param name="key">The optional primary key field name.</param>
    /// <returns>The queried result set, or a <see cref="DataResult"/> with count when counts are requested.</returns>
    public override async Task<object> ReadAsync(DataManagerRequest dataManagerRequest, string? key = null)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var dataSource = dbContext
                .Torrents
                .AsNoTracking()
                .OrderByDescending(x => x.IngestedAt)
                .AsQueryable();

            if (dataManagerRequest.Search is { Count: > 0 })
            {
                dataSource = DataOperations.PerformSearching(dataSource, dataManagerRequest.Search);
            }

            if (dataManagerRequest.Where is { Count: > 0 })
            {
                dataSource = DataOperations.PerformFiltering(dataSource, dataManagerRequest.Where,
                    dataManagerRequest.Where[0].Operator);
            }

            if (dataManagerRequest.Sorted is { Count: > 0 })
            {
                dataSource = DataOperations.PerformSorting(dataSource, dataManagerRequest.Sorted);
            }

            int count = dataSource.Count();

            if (dataManagerRequest.Skip != 0)
            {
                dataSource = DataOperations.PerformSkip(dataSource, dataManagerRequest.Skip);
            }

            if (dataManagerRequest.Take != 0)
            {
                dataSource = DataOperations.PerformTake(dataSource, dataManagerRequest.Take);
            }

            var results = await dataSource
                .Select(x => DashboardTorrentDetails.FromTorrentInfo(x))
                .ToListAsync();

            return !dataManagerRequest.RequiresCounts
                ? results
                : new DataResult
            {
                Result = results,
                Count = count,
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error reading data");
            throw;
        }
    }

    /// <summary>
    /// Inserts a new torrent record derived from the supplied <see cref="DashboardTorrentDetails"/>.
    /// </summary>
    /// <param name="dataManager">The Syncfusion data manager.</param>
    /// <param name="value">The <see cref="DashboardTorrentDetails"/> to insert.</param>
    /// <param name="key">The primary key field name.</param>
    /// <returns>The inserted value, or <c>null</c> when the value is not a <see cref="DashboardTorrentDetails"/>.</returns>
    public override async Task<object> InsertAsync(DataManager dataManager, object? value, string key)
    {
        try
        {
            if (value is not DashboardTorrentDetails incoming)
            {
                return null;
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var torrent = DashboardTorrentDetails.ToTorrentInfo(incoming);
            torrent = await UpdateTorrentAttributes(incoming, torrent, true);

            await dbContext.Torrents.AddAsync(torrent);
            await dbContext.SaveChangesAsync();
            return value;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error inserting data");
            throw;
        }
    }

    /// <summary>
    /// Updates an existing torrent record matching the info hash of the supplied <see cref="DashboardTorrentDetails"/>.
    /// </summary>
    /// <param name="dataManager">The Syncfusion data manager.</param>
    /// <param name="value">The <see cref="DashboardTorrentDetails"/> carrying the updated values.</param>
    /// <param name="keyField">The key field name used to locate the record.</param>
    /// <param name="key">The key value of the record to update.</param>
    /// <returns>The updated value, or <c>null</c> when the record is not found.</returns>
    public override async Task<object> UpdateAsync(DataManager dataManager, object? value, string keyField, string key)
    {
        try
        {
            if (value is not DashboardTorrentDetails incoming)
            {
                return null;
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var torrent = await dbContext.Torrents.AsNoTracking().FirstOrDefaultAsync(x=> x.InfoHash == incoming.InfoHash);
            if (torrent == null)
            {
                return null;
            }

            torrent.RawTitle = incoming.RawTitle;
            torrent.Size = incoming.Size;
            torrent = await UpdateTorrentAttributes(incoming, torrent);

            dbContext.Torrents.Update(torrent);
            await dbContext.SaveChangesAsync();
            return value;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error updating data");
            throw;
        }
    }

    /// <summary>
    /// Removes the torrent record whose info hash matches the supplied key.
    /// </summary>
    /// <param name="dataManager">The Syncfusion data manager.</param>
    /// <param name="value">The info hash string identifying the record to remove.</param>
    /// <param name="keyField">The key field name.</param>
    /// <param name="key">The key value.</param>
    /// <returns>The removed value, or throws when no record was deleted.</returns>
    public override async Task<object> RemoveAsync(DataManager dataManager, object? value, string keyField, string key)
    {
        try
        {
            if (value is not string incoming)
            {
                return null;
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var result = await dbContext.Torrents.Where(x=>x.InfoHash == incoming).ExecuteDeleteAsync();
            return result == 0 ? throw new InvalidOperationException("No records were deleted") : value;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error removing data");
            throw;
        }
    }

    /// <summary>
    /// Performs a batch update, currently handling only deletion of the supplied records.
    /// </summary>
    /// <param name="dataManager">The Syncfusion data manager.</param>
    /// <param name="changed">Records with changed values (not currently processed).</param>
    /// <param name="added">Records to add (not currently processed).</param>
    /// <param name="deleted">The <see cref="DashboardTorrentDetails"/> records to delete.</param>
    /// <param name="keyField">The key field name.</param>
    /// <param name="key">The key value.</param>
    /// <param name="dropIndex">The optional drop index for reordered records.</param>
    /// <returns>The key when deletions succeed, or throws when no or partial deletions occur.</returns>
    public override async Task<object> BatchUpdateAsync(DataManager dataManager, object? changed, object? added, object? deleted,
        string keyField, string key, int? dropIndex)
    {
        try
        {
            if (deleted == null)
            {
                return key;
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var deletedIds = ((IEnumerable<DashboardTorrentDetails>)deleted).Select(x => x.InfoHash).ToList();
            var result = await dbContext.Torrents.Where(x => deletedIds.Contains(x.InfoHash)).ExecuteDeleteAsync();
            return result == 0
                ? throw new InvalidOperationException("No records were deleted")
                : result != deletedIds.Count ? throw new InvalidOperationException("Not all records were deleted") : (object)key;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error batch updating data");
            throw;
        }
    }

    private async Task<TorrentInfo> UpdateTorrentAttributes(DashboardTorrentDetails incoming, TorrentInfo torrent, bool isCreate = false)
    {
        try
        {
            torrent = await parseTorrentNameService.ParseAndPopulateTorrentInfoAsync(torrent);
            torrent.CleanedParsedTitle = Parsing.CleanQuery(torrent.ParsedTitle);
        }
        catch (Exception) when (!pythonRuntimeService.IsAvailable)
        {
            logger.LogWarning("Python engine unavailable - dashboard re-parse disabled. Keeping existing torrent data.");
        }

        if (isCreate)
        {
            torrent.IngestedAt = DateTime.UtcNow;
            return torrent;
        }

        if (incoming.ChangeCategory || (!incoming.Category.IsNullOrWhiteSpace() && incoming.Category != torrent.Category))
        {
            torrent.Category = incoming.Category;
        }

        if (incoming.ChangeTrash || incoming.Trash != torrent.Trash)
        {
            torrent.Trash = incoming.Trash;
        }

        if (incoming.ChangeYear || (!incoming.Year.IsNullOrWhiteSpace() && incoming.Year != torrent.Year.ToString()))
        {
            torrent.Year = incoming.Year.IsNullOrWhiteSpace() ? null : int.Parse(incoming.Year);
        }

        if (incoming.ChangeAdult || incoming.IsAdult != torrent.IsAdult)
        {
            torrent.IsAdult = incoming.IsAdult;
        }

        if (incoming.ChangeImdb || incoming.ImdbId != torrent.ImdbId)
        {
            torrent.ImdbId = incoming.ImdbId;
        }

        return torrent;
    }
}