namespace Zilean.Database.Services;

public interface ITorrentsQueryService
{
    Task<IReadOnlyList<CachedItem>> CheckCachedAsync(string[] hashes, int maxHashes, CancellationToken ct);
    IAsyncEnumerable<StreamedEntry> StreamAllAsync(CancellationToken ct);
}