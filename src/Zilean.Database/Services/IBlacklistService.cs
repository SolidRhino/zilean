namespace Zilean.Database.Services;

public interface IBlacklistService
{
    Task<BlacklistResult> AddAsync(string infoHash, string reason, CancellationToken ct);
    Task<BlacklistResult> RemoveAsync(string infoHash, CancellationToken ct);
}

public enum BlacklistResult
{
    Added,
    AlreadyBlacklisted,
    NotFound,
    InvalidHash,
    InvalidReason,
}