namespace Zilean.Shared.Features.Scraping;

/// <summary>
/// Represents a single streamed entry (file/release) returned by a generic ingestion endpoint.
/// </summary>
public class StreamedEntry
{
    /// <summary>
    /// Gets or sets the display name of the streamed entry.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the size of the entry in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public required long Size { get; set; }

    /// <summary>
    /// Gets or sets the info hash identifying the torrent.
    /// </summary>
    [JsonPropertyName("hash")]
    public required string InfoHash { get; set; }

    /// <summary>
    /// Gets or sets the parsed <see cref="TorrentInfo"/> populated from this entry's name, if parsing succeeded.
    /// </summary>
    public TorrentInfo? ParseResponse { get; set; }
}