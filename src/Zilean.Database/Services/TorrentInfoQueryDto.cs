namespace Zilean.Database.Services;

/// <summary>
/// Flat DTO for mapping <c>search_torrents_meta</c> results via <c>SqlQueryRaw</c>.
/// Does NOT inherit <see cref="TorrentInfo"/> to avoid the <c>Imdb</c> navigation property
/// that EF Core rejects for <c>SqlQueryRaw</c>. Mapped to <see cref="TorrentInfoResult"/>
/// after materialization.
/// </summary>
public class TorrentInfoQueryDto
{
    /// <summary>Gets the BitTorrent info hash identifying this torrent.</summary>
    public string InfoHash { get; set; } = default!;
    /// <summary>Gets the parsed release resolution (e.g. 1080p, 720p).</summary>
    public string? Resolution { get; set; }
    /// <summary>Gets the release year parsed from the torrent title.</summary>
    public int? Year { get; set; }
    /// <summary>Gets a value indicating whether this is a remastered release.</summary>
    public bool? Remastered { get; set; }
    /// <summary>Gets the video codec (e.g. x264, x265, HEVC).</summary>
    public string? Codec { get; set; }
    /// <summary>Gets the audio format tags (e.g. AAC, DTS, Atmos).</summary>
    public string[]? Audio { get; set; }
    /// <summary>Gets the release quality/source (e.g. BluRay, WEB-DL).</summary>
    public string? Quality { get; set; }
    /// <summary>Gets the episode numbers parsed from the torrent title.</summary>
    public int[]? Episodes { get; set; }
    /// <summary>Gets the season numbers parsed from the torrent title.</summary>
    public int[]? Seasons { get; set; }
    /// <summary>Gets the spoken languages detected in the release.</summary>
    public string[]? Languages { get; set; }
    /// <summary>Gets the cleaned, parsed release title.</summary>
    public string? ParsedTitle { get; set; }
    /// <summary>Gets the normalized title used for matching and comparison.</summary>
    public string? NormalizedTitle { get; set; }
    /// <summary>Gets the original, unparsed release title.</summary>
    public string? RawTitle { get; set; }
    /// <summary>Gets the human-readable size string (e.g. "4.5 GB").</summary>
    public string? Size { get; set; }
    /// <summary>Gets the Torznab category identifier for this release.</summary>
    public string Category { get; set; } = default!;
    /// <summary>Gets a value indicating whether the release is a complete pack.</summary>
    public bool? Complete { get; set; }
    /// <summary>Gets the volume numbers parsed from the torrent title.</summary>
    public int[]? Volumes { get; set; }
    /// <summary>Gets the HDR format tags (e.g. HDR10, Dolby Vision).</summary>
    public string[]? Hdr { get; set; }
    /// <summary>Gets the audio channel configurations (e.g. 5.1, 7.1).</summary>
    public string[]? Channels { get; set; }
    /// <summary>Gets a value indicating whether the audio is dubbed.</summary>
    public bool? Dubbed { get; set; }
    /// <summary>Gets a value indicating whether subtitles are hardcoded (subbed).</summary>
    public bool? Subbed { get; set; }
    /// <summary>Gets the release edition (e.g. Directors Cut, Special Edition).</summary>
    public string? Edition { get; set; }
    /// <summary>Gets the bit depth of the video (e.g. 8-bit, 10-bit).</summary>
    public string? BitDepth { get; set; }
    /// <summary>Gets the bitrate information parsed from the release.</summary>
    public string? Bitrate { get; set; }
    /// <summary>Gets the originating network or streaming service.</summary>
    public string? Network { get; set; }
    /// <summary>Gets a value indicating whether this is an extended cut.</summary>
    public bool? Extended { get; set; }
    /// <summary>Gets a value indicating whether the release is a converted/re-encoded version.</summary>
    public bool? Converted { get; set; }
    /// <summary>Gets a value indicating whether subtitles are hardcoded into the video.</summary>
    public bool? Hardcoded { get; set; }
    /// <summary>Gets the region code (e.g. R1, R2).</summary>
    public string? Region { get; set; }
    /// <summary>Gets a value indicating whether this is a pay-per-view release.</summary>
    public bool? Ppv { get; set; }
    /// <summary>Gets a value indicating whether this is a 3D release.</summary>
    public bool? Is3d { get; set; }
    /// <summary>Gets the source site or tracker name.</summary>
    public string? Site { get; set; }
    /// <summary>Gets a value indicating whether this is a proper release (fixed encoding issues).</summary>
    public bool? Proper { get; set; }
    /// <summary>Gets a value indicating whether this is a repack release (fixed content issues).</summary>
    public bool? Repack { get; set; }
    /// <summary>Gets a value indicating whether this is a retail release.</summary>
    public bool? Retail { get; set; }
    /// <summary>Gets a value indicating whether the release was upscaled from a lower resolution.</summary>
    public bool? Upscaled { get; set; }
    /// <summary>Gets a value indicating whether this is an unrated version.</summary>
    public bool? Unrated { get; set; }
    /// <summary>Gets a value indicating whether the release is a documentary.</summary>
    public bool? Documentary { get; set; }
    /// <summary>Gets the raw episode code string (e.g. S01E02).</summary>
    public string? EpisodeCode { get; set; }
    /// <summary>Gets the country of origin.</summary>
    public string? Country { get; set; }
    /// <summary>Gets the container format (e.g. MKV, MP4, AVI).</summary>
    public string? Container { get; set; }
    /// <summary>Gets the file extension.</summary>
    public string? Extension { get; set; }
    /// <summary>Gets a value indicating whether the entry is a torrent (vs. other debrid sources).</summary>
    public bool? Torrent { get; set; }
    /// <summary>Gets the trigram similarity match score from the search query.</summary>
    public float? Score { get; set; }
    /// <summary>Gets the matched IMDb identifier (e.g. tt1234567).</summary>
    public string? ImdbId { get; set; }
    /// <summary>Gets the IMDb category (e.g. movie, tvSeries).</summary>
    public string? ImdbCategory { get; set; }
    /// <summary>Gets the IMDb title for the matched entry.</summary>
    public string? ImdbTitle { get; set; }
    /// <summary>Gets the IMDb release year.</summary>
    public int? ImdbYear { get; set; }
    /// <summary>Gets a value indicating whether the IMDb entry is adult content.</summary>
    public bool? ImdbAdult { get; set; }
    /// <summary>Gets the UTC timestamp when this torrent was ingested.</summary>
    public DateTime IngestedAt { get; set; }

    /// <summary>
    /// Maps this flat DTO to a <see cref="TorrentInfoResult"/> instance.
    /// </summary>
    /// <returns>A <see cref="TorrentInfoResult"/> populated with this DTO's values.</returns>
    public TorrentInfoResult ToTorrentInfoResult() =>
        new()
        {
            InfoHash = InfoHash,
            Resolution = Resolution,
            Year = Year,
            Remastered = Remastered,
            Codec = Codec,
            Audio = Audio,
            Quality = Quality,
            Episodes = Episodes,
            Seasons = Seasons,
            Languages = Languages,
            ParsedTitle = ParsedTitle,
            NormalizedTitle = NormalizedTitle,
            RawTitle = RawTitle,
            Size = Size,
            Category = Category,
            Complete = Complete,
            Volumes = Volumes,
            Hdr = Hdr,
            Channels = Channels,
            Dubbed = Dubbed,
            Subbed = Subbed,
            Edition = Edition,
            BitDepth = BitDepth,
            Bitrate = Bitrate,
            Network = Network,
            Extended = Extended,
            Converted = Converted,
            Hardcoded = Hardcoded,
            Region = Region,
            Ppv = Ppv,
            Is3d = Is3d,
            Site = Site,
            Proper = Proper,
            Repack = Repack,
            Retail = Retail,
            Upscaled = Upscaled,
            Unrated = Unrated,
            Documentary = Documentary,
            EpisodeCode = EpisodeCode,
            Country = Country,
            Container = Container,
            Extension = Extension,
            Torrent = Torrent,
            ImdbId = ImdbId,
            ImdbCategory = ImdbCategory,
            ImdbTitle = ImdbTitle,
            ImdbYear = ImdbYear,
            ImdbAdult = ImdbAdult ?? false,
            IngestedAt = IngestedAt,
        };
}