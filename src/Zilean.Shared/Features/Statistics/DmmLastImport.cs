namespace Zilean.Shared.Features.Statistics;

/// <summary>
/// Tracks metadata about the most recent DMM (DebridMediaManager) import operation.
/// </summary>
public class DmmLastImport : BaseLastImport
{
    /// <summary>
    /// The total number of pages processed during the import.
    /// </summary>
    public long PageCount { get; set; }

    /// <summary>
    /// The total number of torrent entries ingested during the import.
    /// </summary>
    public long EntryCount { get; set; }
}