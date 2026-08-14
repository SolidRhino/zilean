namespace Zilean.Shared.Features.Statistics;

/// <summary>
/// Well-known keys for <see cref="ImportMetadata"/> entries stored in the statistics table.
/// </summary>
public static class MetadataKeys
{
    /// <summary>
    /// The metadata key for the last IMDb import operation.
    /// </summary>
    public const string ImdbLastImport = "ImdbLastImport";

    /// <summary>
    /// The metadata key for the last DMM import operation.
    /// </summary>
    public const string DmmLastImport = "DmmLastImport";
}