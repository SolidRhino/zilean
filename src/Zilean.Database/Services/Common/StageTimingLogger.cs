namespace Zilean.Database.Services.Common;

/// <summary>
/// Source-generated logger helper for per-stage ingestion timing measurements.
/// </summary>
public static partial class StageTimingLogger
{
    /// <summary>
    /// Logs the measured duration of each ingestion stage (parse, populate, match, upsert) for a batch.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="batchNumber">The zero-based batch index being processed.</param>
    /// <param name="parseMs">Time spent parsing torrents, in milliseconds.</param>
    /// <param name="populateMs">Time spent populating IMDb data, in milliseconds.</param>
    /// <param name="matchMs">Time spent matching IMDb IDs, in milliseconds.</param>
    /// <param name="upsertMs">Time spent upserting results, in milliseconds.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Batch {BatchNumber} stage timings: parse={ParseMs}ms populate={PopulateMs}ms match={MatchMs}ms upsert={UpsertMs}ms")]
    public static partial void LogBatchStageTimings(
        this ILogger logger,
        int batchNumber,
        long parseMs,
        long populateMs,
        long matchMs,
        long upsertMs);
}
