namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Provides constants and helpers for resolving the maximum number of concurrent
/// parser tasks during ingestion.
/// </summary>
public static class ParserConcurrency
{
    /// <summary>
    /// Hard upper bound on the number of thread-pool workers used for concurrent parsing.
    /// </summary>
    public const int MaxThreadPoolWorkers = 8;

    /// <summary>
    /// Resolves the effective maximum number of concurrent parser tasks, capped by
    /// <see cref="MaxThreadPoolWorkers"/> and the available <c>Environment.ProcessorCount</c>.
    /// </summary>
    /// <returns>The lesser of <see cref="MaxThreadPoolWorkers"/> and <c>Environment.ProcessorCount</c>.</returns>
    public static int ResolveMaxConcurrentTasks() =>
        Math.Min(Environment.ProcessorCount, MaxThreadPoolWorkers);
}