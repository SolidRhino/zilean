namespace Zilean.Database.Services;

public class TorrentsQueryService(IDbContextFactory<ZileanDbContext> dbContextFactory) : ITorrentsQueryService
{
    public async Task<IReadOnlyList<CachedItem>> CheckCachedAsync(string[] hashes, int maxHashes, CancellationToken ct)
    {
        if (hashes.Length > maxHashes)
        {
            throw new ArgumentException($"Too many hashes provided. The limit is {maxHashes}.");
        }

        var hashSet = new HashSet<string>(
            hashes.Select(h => h.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);

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

        await foreach (var record in dbContext.Torrents
                           .Select(r => new { r.RawTitle, r.InfoHash, r.Size })
                           .AsAsyncEnumerable()
                           .WithCancellation(ct))
        {
            yield return new StreamedEntry
            {
                Name = record.RawTitle,
                InfoHash = record.InfoHash,
                Size = long.TryParse(new string((record.Size ?? string.Empty).TakeWhile(char.IsDigit).ToArray()), out var sz) ? sz : 0,
            };
        }
    }
}