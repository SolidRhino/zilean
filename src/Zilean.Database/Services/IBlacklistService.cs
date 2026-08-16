namespace Zilean.Database.Services;

/// <summary>
/// Provides operations for managing blacklisted torrent info hashes.
/// </summary>
public interface IBlacklistService
{
    /// <summary>
    /// Adds an info hash to the blacklist, removing the corresponding torrent if present.
    /// </summary>
    /// <param name="infoHash">The BitTorrent info hash to blacklist.</param>
    /// <param name="reason">The human-readable reason for blacklisting.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>A <see cref="BlacklistResult"/> indicating the outcome.</returns>
    Task<BlacklistResult> AddAsync(string infoHash, string reason, CancellationToken ct);

    /// <summary>
    /// Removes an info hash from the blacklist.
    /// </summary>
    /// <param name="infoHash">The BitTorrent info hash to remove from the blacklist.</param>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>A <see cref="BlacklistResult"/> indicating the outcome.</returns>
    Task<BlacklistResult> RemoveAsync(string infoHash, CancellationToken ct);

    /// <summary>
    /// Retrieves all blacklisted items.
    /// </summary>
    /// <param name="ct">Token to cancel the operation.</param>
    /// <returns>A list of all <see cref="BlacklistedItem"/> entries.</returns>
    Task<List<BlacklistedItem>> ListAsync(CancellationToken ct);
}

/// <summary>
/// Represents the outcome of a blacklist add or remove operation.
/// </summary>
public enum BlacklistResult
{
    /// <summary>The info hash was successfully added to the blacklist.</summary>
    Added,
    /// <summary>The info hash was successfully removed from the blacklist.</summary>
    Removed,
    /// <summary>The info hash is already present in the blacklist.</summary>
    AlreadyBlacklisted,
    /// <summary>The info hash was not found in the blacklist.</summary>
    NotFound,
    /// <summary>The provided info hash is empty or invalid.</summary>
    InvalidHash,
    /// <summary>The provided reason is empty or invalid.</summary>
    InvalidReason,
}