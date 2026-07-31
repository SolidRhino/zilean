namespace Zilean.Database.Services;

public class TorrentsQueryService(IDbContextFactory<ZileanDbContext> dbContextFactory) : ITorrentsQueryService
{
    public async Task<IReadOnlyList<CachedItem>> CheckCachedAsync(string[] hashes, int maxHashes, CancellationToken ct)
    {
        if (hashes.Length >= maxHashes)
        {
            throw new ArgumentException($"Too many hashes provided. The limit is {maxHashes}.");
        }

        var hashSet = new HashSet<string>(hashes);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var items = await dbContext
            .Torrents
            .AsNoTracking()
            .Where(record => hashSet.Contains(record.InfoHash))
            .Select(record => new CachedItem
            {
                InfoHash = record.InfoHash,
                IsCached = true,
                Item = record
            })
            .ToListAsync(ct);

        var matchedHashes = new HashSet<string>(
            items.Select(x => x.InfoHash!),
            StringComparer.OrdinalIgnoreCase);

        foreach (var hash in hashSet)
        {
            if (matchedHashes.Contains(hash))
            {
                continue;
            }

            items.Add(new CachedItem
            {
                InfoHash = hash,
                IsCached = false,
                Item = null
            });
        }

        return items;
    }

    public async IAsyncEnumerable<StreamedEntry> StreamAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        await foreach (var item in dbContext.Torrents
                           .Select(record => new StreamedEntry
                           {
                               Name = record.RawTitle,
                               InfoHash = record.InfoHash,
                               Size = long.Parse(record.Size),
                           })
                           .AsAsyncEnumerable()
                           .WithCancellation(ct))
        {
            yield return item;
        }
    }
}