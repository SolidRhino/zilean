namespace Zilean.Scraper.Features.Ingestion.Processing;

/// <summary>
/// Tracks counts of processed, filtered, and removed entries during ingestion.
/// </summary>
public sealed class ProcessedCounts
{
    private int _totalProcessed;
    private int _adultRemoved;
    private int _trashRemoved;
    private int _blacklistedRemoved;

    /// <summary>
    /// Resets all counters to zero.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _totalProcessed, 0);
        Interlocked.Exchange(ref _adultRemoved, 0);
        Interlocked.Exchange(ref _trashRemoved, 0);
        Interlocked.Exchange(ref _blacklistedRemoved, 0);
    }

    /// <summary>
    /// Gets the total number of entries processed.
    /// </summary>
    public int TotalProcessed => _totalProcessed;
    /// <summary>
    /// Adds the specified count to the total processed counter.
    /// </summary>
    /// <param name="count">The number of entries to add.</param>
    public void AddProcessed(int count) => Interlocked.Add(ref _totalProcessed, count);
    /// <summary>
    /// Adds the specified count to the adult-content-removed counter.
    /// </summary>
    /// <param name="count">The number of adult entries removed.</param>
    public void AddAdultRemoved(int count) => Interlocked.Add(ref _adultRemoved, count);
    /// <summary>
    /// Adds the specified count to the trash-removed counter.
    /// </summary>
    /// <param name="count">The number of trash entries removed.</param>
    public void AddTrashRemoved(int count) => Interlocked.Add(ref _trashRemoved, count);
    /// <summary>
    /// Adds the specified count to the blacklisted-removed counter.
    /// </summary>
    /// <param name="count">The number of blacklisted entries removed.</param>
    public void AddBlacklistedRemoved(int count) => Interlocked.Add(ref _blacklistedRemoved, count);

    /// <summary>
    /// Writes a summary table of processed counts to the console.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="stopwatch">The stopwatch measuring elapsed processing time.</param>
    /// <param name="newPages">Optional mapping of new DMM pages to entry counts.</param>
    /// <param name="endpoint">Optional generic endpoint being processed.</param>
    public void WriteOutput(ZileanConfiguration configuration, Stopwatch stopwatch, ConcurrentDictionary<string, int>? newPages = null, GenericEndpoint? endpoint = null)
    {
        var table = new Table();

        table.AddColumn("Description");
        table.AddColumn("Count");
        table.AddColumn("Additional Info");

        if (newPages is not null)
        {
            table.AddRow("Processed new DMM pages", newPages.Count.ToString(), $"{newPages.Sum(x => x.Value)} entries");
        }

        if (endpoint is not null)
        {
            table.AddRow("Processed URL", endpoint.Url, $"Type: {endpoint.EndpointType}");
        }

        table.AddRow("Processed torrents", _totalProcessed.ToString(), $"Time Taken: {stopwatch.Elapsed.TotalSeconds:F2}s");

        if (_blacklistedRemoved > 0)
        {
            table.AddRow("Removed Blacklisted Content", _blacklistedRemoved.ToString(), "Due to identification by infohash");
        }

        AnsiConsole.Write(table);
    }
}
