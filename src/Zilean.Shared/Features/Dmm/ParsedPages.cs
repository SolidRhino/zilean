namespace Zilean.Shared.Features.Dmm;

/// <summary>
/// Tracks the ingestion progress of parsed DMM pages, keyed by page identifier.
/// </summary>
public class ParsedPages
{
    /// <summary>
    /// The unique page identifier serving as the primary key.
    /// </summary>
    [Key]
    public string Page { get; set; } = default!;

    /// <summary>
    /// The number of torrent entries ingested from this page.
    /// </summary>
    public int EntryCount { get; set; }
}
