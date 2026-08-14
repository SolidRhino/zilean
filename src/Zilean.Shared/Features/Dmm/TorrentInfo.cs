namespace Zilean.Shared.Features.Dmm;

/// <summary>
/// Represents a parsed torrent release entry from DebridMediaManager ingestion.
/// Each instance maps to one parsed RTN result stored in the PostgreSQL database.
/// </summary>
public class TorrentInfo
{
    /// <summary>
    /// The original unparsed release name as it appeared in the DMM stream.
    /// </summary>
    [JsonPropertyName("raw_title")]
    public string? RawTitle { get; set; }

    /// <summary>
    /// The human-readable title extracted from the release name after parsing.
    /// </summary>
    [JsonPropertyName("parsed_title")]
    public string? ParsedTitle { get; set; }

    /// <summary>
    /// The title normalized for matching against IMDb and other metadata sources.
    /// </summary>
    [JsonPropertyName("normalized_title")]
    public string? NormalizedTitle { get; set; }

    /// <summary>
    /// The parsed title with punctuation and noise removed for search indexing.
    /// </summary>
    [JsonPropertyName("cleaned_parsed_title")]
    public string? CleanedParsedTitle { get; set; }

    /// <summary>
    /// Indicates whether the release was flagged as trash by the RTN parser.
    /// </summary>
    [JsonPropertyName("trash")]
    public bool? Trash { get; set; } = false;

    /// <summary>
    /// The release year parsed from the release name, if detected.
    /// </summary>
    [JsonPropertyName("year")]
    public int? Year { get; set; } = 0;

    /// <summary>
    /// The video resolution (e.g. <c>1080p</c>, <c>4K</c>) parsed from the release name.
    /// </summary>
    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; }

    /// <summary>
    /// The season numbers parsed from the release name, if applicable to a TV series.
    /// </summary>
    [JsonPropertyName("seasons")]
    public int[]? Seasons { get; set; } = [];

    /// <summary>
    /// The episode numbers parsed from the release name, if applicable to a TV series.
    /// </summary>
    [JsonPropertyName("episodes")]
    public int[]? Episodes { get; set; } = [];

    /// <summary>
    /// Indicates whether the release is a complete season/series pack.
    /// </summary>
    [JsonPropertyName("complete")]
    public bool? Complete { get; set; } = false;

    /// <summary>
    /// The volume numbers parsed from the release name, when present.
    /// </summary>
    [JsonPropertyName("volumes")]
    public int[]? Volumes { get; set; } = [];

    /// <summary>
    /// The spoken languages parsed from the release name (e.g. <c>ENG</c>, <c>JPN</c>).
    /// </summary>
    [JsonPropertyName("languages")]
    public string[]? Languages { get; set; } = [];

    /// <summary>
    /// The overall release quality (e.g. <c>WEB-DL</c>, <c>BluRay</c>).
    /// </summary>
    [JsonPropertyName("quality")]
    public string? Quality { get; set; }

    /// <summary>
    /// The HDR variants detected in the release (e.g. <c>HDR10</c>, <c>Dolby Vision</c>).
    /// </summary>
    [JsonPropertyName("hdr")]
    public string[]? Hdr { get; set; } = [];

    /// <summary>
    /// The video codec (e.g. <c>x265</c>, <c>H.264</c>) parsed from the release name.
    /// </summary>
    [JsonPropertyName("codec")]
    public string? Codec { get; set; }

    /// <summary>
    /// The audio formats parsed from the release name (e.g. <c>AC3</c>, <c>DTS</c>).
    /// </summary>
    [JsonPropertyName("audio")]
    public string[]? Audio { get; set; } = [];

    /// <summary>
    /// The audio channel configurations (e.g. <c>5.1</c>, <c>7.1</c>).
    /// </summary>
    [JsonPropertyName("channels")]
    public string[]? Channels { get; set; } = [];

    /// <summary>
    /// Indicates whether the release contains a dubbed audio track.
    /// </summary>
    [JsonPropertyName("dubbed")]
    public bool? Dubbed { get; set; } = false;

    /// <summary>
    /// Indicates whether the release contains embedded subtitles.
    /// </summary>
    [JsonPropertyName("subbed")]
    public bool? Subbed { get; set; } = false;

    /// <summary>
    /// The release date string as parsed from the release name, if present.
    /// </summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>
    /// The release group credited in the release name (e.g. <c>NTb</c>, <c>RARBG</c>).
    /// </summary>
    [JsonPropertyName("group")]
    public string? Group { get; set; }

    /// <summary>
    /// The release edition (e.g. <c>Directors Cut</c>, <c>Extended</c>).
    /// </summary>
    [JsonPropertyName("edition")]
    public string? Edition { get; set; }

    /// <summary>
    /// The bit depth of the video (e.g. <c>8-bit</c>, <c>10-bit</c>).
    /// </summary>
    [JsonPropertyName("bit_depth")]
    public string? BitDepth { get; set; }

    /// <summary>
    /// The bitrate of the release as parsed from the release name, if present.
    /// </summary>
    [JsonPropertyName("bitrate")]
    public string? Bitrate { get; set; }

    /// <summary>
    /// The originating network or streaming service (e.g. <c>Netflix</c>, <c>HBO</c>).
    /// </summary>
    [JsonPropertyName("network")]
    public string? Network { get; set; }

    /// <summary>
    /// Indicates whether the release is an extended version of the content.
    /// </summary>
    [JsonPropertyName("extended")]
    public bool? Extended { get; set; } = false;

    /// <summary>
    /// Indicates whether the release is a converted/transcoded version.
    /// </summary>
    [JsonPropertyName("converted")]
    public bool? Converted { get; set; } = false;

    /// <summary>
    /// Indicates whether the release contains hardcoded (burned-in) subtitles.
    /// </summary>
    [JsonPropertyName("hardcoded")]
    public bool? Hardcoded { get; set; } = false;

    /// <summary>
    /// The region code parsed from the release name (e.g. <c>R1</c>, <c>B</c>).
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    /// <summary>
    /// Indicates whether the release is pay-per-view content.
    /// </summary>
    [JsonPropertyName("ppv")]
    public bool? Ppv { get; set; } = false;

    /// <summary>
    /// Indicates whether the release is a 3D presentation.
    /// </summary>
    [JsonPropertyName("_3d")]
    public bool? Is3d { get; set; } = false;

    /// <summary>
    /// The source website or tracker of the release, if identified.
    /// </summary>
    [JsonPropertyName("site")]
    public string? Site { get; set; }

    /// <summary>
    /// The file size string as parsed from the release name (e.g. <c>4.5 GB</c>).
    /// </summary>
    [JsonPropertyName("size")]
    public string? Size { get; set; }

    /// <summary>
    /// Indicates whether the release is a proper (corrected re-release).
    /// </summary>
    [JsonPropertyName("proper")]
    public bool? Proper { get; set; } = false;

    /// <summary>
    /// Indicates whether the release is a repack (re-encoded to fix issues).
    /// </summary>
    [JsonPropertyName("repack")]
    public bool? Repack { get; set; } = false;

    /// <summary>
    /// Indicates whether the release is a retail version of the content.
    /// </summary>
    [JsonPropertyName("retail")]
    public bool? Retail { get; set; } = false;

    /// <summary>
    /// Indicates whether the release has been upscaled from a lower resolution.
    /// </summary>
    [JsonPropertyName("upscaled")]
    public bool? Upscaled { get; set; } = false;

    /// <summary>
    /// Indicates whether the release is a remastered version of the content.
    /// </summary>
    [JsonPropertyName("remastered")]
    public bool? Remastered { get; set; } = false;

    /// <summary>
    /// Indicates whether the release is an unrated version of the content.
    /// </summary>
    [JsonPropertyName("unrated")]
    public bool? Unrated { get; set; } = false;

    /// <summary>
    /// Indicates whether the release is documentary content.
    /// </summary>
    [JsonPropertyName("documentary")]
    public bool? Documentary { get; set; } = false;

    /// <summary>
    /// The episode code (e.g. <c>S01E05</c>) parsed from the release name.
    /// </summary>
    [JsonPropertyName("episode_code")]
    public string? EpisodeCode { get; set; }

    /// <summary>
    /// The country of origin parsed from the release name, if present.
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; set; }

    /// <summary>
    /// The container format (e.g. <c>MKV</c>, <c>MP4</c>) parsed from the release name.
    /// </summary>
    [JsonPropertyName("container")]
    public string? Container { get; set; }

    /// <summary>
    /// The file extension parsed from the release name, if present.
    /// </summary>
    [JsonPropertyName("extension")]
    public string? Extension { get; set; }

    /// <summary>
    /// Indicates whether the entry represents a torrent (vs. other source types).
    /// </summary>
    [JsonPropertyName("torrent")]
    public bool? Torrent { get; set; } = false;

    /// <summary>
    /// The Torznab category identifier mapped from the parsed content type
    /// (e.g. <c>2000</c> for Movies, <c>5000</c> for TV).
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = default!;

    /// <summary>
    /// The IMDb identifier (e.g. <c>tt1234567</c>) linked to this release, if matched.
    /// </summary>
    [JsonPropertyName("imdb_id")]
    public string? ImdbId { get; set; }

    /// <summary>
    /// Navigation property to the matched <see cref="ImdbFile"/> metadata, if available.
    /// </summary>
    [JsonPropertyName("imdb")]
    public virtual ImdbFile? Imdb { get; set; }

    /// <summary>
    /// The unique info hash (SHA1/magnet hash) identifying this torrent.
    /// </summary>
    [JsonPropertyName("info_hash")]
    public string InfoHash { get; set; } = default!;

    /// <summary>
    /// Indicates whether the release is adult (XXX) content.
    /// </summary>
    [JsonPropertyName("adult")]
    public bool IsAdult { get; set; }

    /// <summary>
    /// The UTC timestamp when this entry was ingested into the database.
    /// </summary>
    [JsonPropertyName("ingested_at")]
    public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
}
