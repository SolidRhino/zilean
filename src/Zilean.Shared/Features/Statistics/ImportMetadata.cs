namespace Zilean.Shared.Features.Statistics;

/// <summary>
/// Stores a key-value import metadata entry, where the value is an arbitrary JSON document.
/// Used to persist import statistics such as last-import timestamps and counts.
/// </summary>
public class ImportMetadata
{
    /// <summary>
    /// The unique key identifying this metadata entry (e.g. <c>DmmLastImport</c>).
    /// </summary>
    [Key]
    public string Key { get; set; } = default!;

    /// <summary>
    /// The JSON document containing the metadata payload.
    /// </summary>
    public JsonDocument Value { get; set; } = JsonDocument.Parse("{}");
}