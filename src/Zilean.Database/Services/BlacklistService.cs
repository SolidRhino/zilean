namespace Zilean.Database.Services;

/// <summary>
/// Default implementation of <see cref="IBlacklistService"/> for managing blacklisted torrent info hashes via EF Core.
/// </summary>
public class BlacklistService(IDbContextFactory<ZileanDbContext> dbContextFactory, ILogger<BlacklistService> logger) : IBlacklistService
{
    /// <inheritdoc/>
    public async Task<BlacklistResult> AddAsync(string infoHash, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(infoHash))
        {
            return BlacklistResult.InvalidHash;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return BlacklistResult.InvalidReason;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        if (await dbContext.BlacklistedItems.AnyAsync(x => x.InfoHash == infoHash, ct))
        {
            return BlacklistResult.AlreadyBlacklisted;
        }

        var blacklistedItem = new BlacklistedItem
        {
            InfoHash = infoHash,
            Reason = reason,
            BlacklistedAt = DateTime.UtcNow
        };

        dbContext.BlacklistedItems.Add(blacklistedItem);

        var torrentInfo = await dbContext.Torrents.FirstOrDefaultAsync(x => x.InfoHash == infoHash, ct);

        if (torrentInfo != null)
        {
            dbContext.Torrents.Remove(torrentInfo);
            logger.LogInformation("Removed torrent {InfoHash} from database", infoHash);
        }

        await dbContext.SaveChangesAsync(ct);

        return BlacklistResult.Added;
    }

    /// <inheritdoc/>
    public async Task<BlacklistResult> RemoveAsync(string infoHash, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(infoHash))
        {
            return BlacklistResult.InvalidHash;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var item = await dbContext.BlacklistedItems.FirstOrDefaultAsync(x => x.InfoHash == infoHash, ct);

        if (item == null)
        {
            return BlacklistResult.NotFound;
        }

        dbContext.BlacklistedItems.Remove(item);
        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Removed blacklisted item {InfoHash}", infoHash);

        return BlacklistResult.Removed;
    }

    /// <inheritdoc/>
    public async Task<List<BlacklistedItem>> ListAsync(CancellationToken ct)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        return await dbContext.BlacklistedItems.AsNoTracking().OrderByDescending(x => x.BlacklistedAt).ToListAsync(ct);
    }
}