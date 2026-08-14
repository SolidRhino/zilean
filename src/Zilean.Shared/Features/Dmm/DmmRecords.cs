namespace Zilean.Shared.Features.Dmm;

/// <summary>
/// Represents a single DMM entry extracted from the ingestion stream,
/// before it has been parsed by RTN.
/// </summary>
/// <param name="infoHash">The torrent info hash, if known at extraction time.</param>
/// <param name="filename">The raw release name/filename from the DMM stream.</param>
/// <param name="filesize">The file size in bytes.</param>
/// <param name="parseResponse">The parsed <see cref="TorrentInfo"/>, if already parsed; otherwise <c>null</c>.</param>
public class ExtractedDmmEntry(string? infoHash, string? filename, long filesize, TorrentInfo? parseResponse)
{
    /// <summary>
    /// The raw release name/filename from the DMM stream.
    /// </summary>
    public string? Filename { get; set; } = filename;

    /// <summary>
    /// The torrent info hash, if known at extraction time.
    /// </summary>
    public string? InfoHash { get; set; } = infoHash;

    /// <summary>
    /// The file size in bytes.
    /// </summary>
    public long Filesize { get; set; } = filesize;

    /// <summary>
    /// The parsed <see cref="TorrentInfo"/>, if already parsed; otherwise <c>null</c>.
    /// </summary>
    public TorrentInfo? ParseResponse { get; set; } = parseResponse;

    /// <summary>
    /// Creates an <see cref="ExtractedDmmEntry"/> from a streamed DMM entry,
    /// with the parse response left as <c>null</c>.
    /// </summary>
    /// <param name="streamedEntry">The streamed entry to convert.</param>
    /// <returns>A new <see cref="ExtractedDmmEntry"/> populated from the streamed entry.</returns>
    public static ExtractedDmmEntry FromStreamedEntry(StreamedEntry streamedEntry) =>
        new(streamedEntry.InfoHash, streamedEntry.Name, streamedEntry.Size, null);
}

/// <summary>
/// Represents a DMM entry response containing the parsed torrent info
/// and its identifying metadata for API output.
/// </summary>
/// <param name="torrentInfo">The parsed torrent info to extract response fields from.</param>
public class ExtractedDmmEntryResponse(TorrentInfo torrentInfo)
{
    /// <summary>
    /// The raw release name/filename.
    /// </summary>
    public string? Filename { get; set; } = torrentInfo.RawTitle;

    /// <summary>
    /// The torrent info hash.
    /// </summary>
    public string? InfoHash { get; set; } = torrentInfo.InfoHash;

    /// <summary>
    /// The file size string.
    /// </summary>
    public string Filesize { get; set; } = torrentInfo.Size;

    /// <summary>
    /// The fully parsed <see cref="TorrentInfo"/>.
    /// </summary>
    public TorrentInfo ParseResponse { get; set; } = torrentInfo;
}

/// <summary>
/// Represents a DMM API query request containing the search text.
/// </summary>
/// <param name="QueryText">The raw query string to search DMM for.</param>
public record DmmQueryRequest(string QueryText);
