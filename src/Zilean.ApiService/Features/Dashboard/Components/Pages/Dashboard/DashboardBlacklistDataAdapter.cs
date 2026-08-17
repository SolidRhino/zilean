namespace Zilean.ApiService.Features.Dashboard.Components.Pages.Dashboard;

/// <summary>
/// Syncfusion <c>DataAdaptor</c> that bridges the blacklist dashboard grid with the
/// <see cref="ZileanDbContext"/>, supporting read, insert and remove operations against
/// <see cref="BlacklistedItem"/> records.
/// </summary>
/// <param name="dbContextFactory">Factory for creating scoped <see cref="ZileanDbContext"/> instances.</param>
/// <param name="logger">Logger for recording data operation failures.</param>
public class DashboardBlacklistDataAdapter(IDbContextFactory<ZileanDbContext> dbContextFactory, ILogger<DashboardBlacklistDataAdapter> logger) : DataAdaptor
{
    /// <summary>
    /// Reads blacklisted item records from the database applying the Syncfusion data manager
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
                .BlacklistedItems
                .AsNoTracking()
                .OrderByDescending(x => x.BlacklistedAt)
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

            var count = dataSource.Count();

            if (dataManagerRequest.Skip != 0)
            {
                dataSource = DataOperations.PerformSkip(dataSource, dataManagerRequest.Skip);
            }

            if (dataManagerRequest.Take != 0)
            {
                dataSource = DataOperations.PerformTake(dataSource, dataManagerRequest.Take);
            }

            var items = await dataSource
                .Select(x => new BlacklistItemDetails
                {
                    InfoHash = x.InfoHash,
                    Reason = x.Reason,
                    BlacklistedAt = x.BlacklistedAt,
                })
                .ToListAsync();

            return !dataManagerRequest.RequiresCounts
                ? items
                : new DataResult
                {
                    Result = items,
                    Count = count,
                };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading blacklist data");
            throw;
        }
    }

    /// <summary>
    /// Inserts a new blacklisted item record from the supplied <see cref="BlacklistItemDetails"/>.
    /// </summary>
    /// <param name="dataManager">The Syncfusion data manager.</param>
    /// <param name="value">The <see cref="BlacklistItemDetails"/> to insert.</param>
    /// <param name="key">The primary key field name.</param>
    /// <returns>The inserted value, or <c>null</c> when the value is not a <see cref="BlacklistItemDetails"/>.</returns>
    public override async Task<object> InsertAsync(DataManager dataManager, object? value, string key)
    {
        try
        {
            if (value is not BlacklistItemDetails incoming)
            {
                return null;
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var item = new BlacklistedItem
            {
                InfoHash = incoming.InfoHash,
                Reason = incoming.Reason,
                BlacklistedAt = DateTime.UtcNow
            };

            await dbContext.BlacklistedItems.AddAsync(item);
            await dbContext.SaveChangesAsync();
            return value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inserting blacklist data");
            throw;
        }
    }

    /// <summary>
    /// Removes the blacklisted item whose info hash matches the supplied key.
    /// </summary>
    /// <param name="dataManager">The Syncfusion data manager.</param>
    /// <param name="value">The info hash string identifying the record to remove.</param>
    /// <param name="keyField">The key field name.</param>
    /// <param name="key">The key value.</param>
    /// <returns>The removed value.</returns>
    public override async Task<object> RemoveAsync(DataManager dataManager, object? value, string keyField, string key)
    {
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            if (value is string infoHash)
            {
                var item = await dbContext.BlacklistedItems.FirstOrDefaultAsync(x => x.InfoHash == infoHash);
                if (item != null)
                {
                    dbContext.BlacklistedItems.Remove(item);
                    await dbContext.SaveChangesAsync();
                }
            }

            return value;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing blacklist data");
            throw;
        }
    }
}