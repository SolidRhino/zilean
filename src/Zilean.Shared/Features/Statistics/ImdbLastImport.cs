namespace Zilean.Shared.Features.Statistics;

/// <summary>
/// Tracks metadata about the most recent IMDb import operation.
/// </summary>
public class ImdbLastImport : BaseLastImport
{
    /// <summary>
    /// The total number of IMDb entries ingested during the import.
    /// </summary>
    public long EntryCount { get; set; }
}