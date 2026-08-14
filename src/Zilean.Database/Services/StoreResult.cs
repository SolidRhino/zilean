namespace Zilean.Database.Services;

/// <summary>
/// Result of a bulk torrent info upsert operation, with timing breakdown.
/// </summary>
/// <param name="Stored">The total number of torrent entries stored (inserted + updated).</param>
/// <param name="PopulateMs">Time spent populating IMDb matching data, in milliseconds.</param>
/// <param name="MatchMs">Time spent matching torrents to IMDb entries, in milliseconds.</param>
/// <param name="UpsertMs">Time spent performing the bulk upsert, in milliseconds.</param>
public readonly record struct StoreResult(int Stored, long PopulateMs, long MatchMs, long UpsertMs);
