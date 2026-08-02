namespace Zilean.Database.Services;

/// <summary>
/// Flat DTO for mapping <c>search_torrents_meta</c> results via <c>SqlQueryRaw</c>.
/// Does NOT inherit <see cref="TorrentInfo"/> to avoid the <c>Imdb</c> navigation property
/// that EF Core rejects for <c>SqlQueryRaw</c>. Mapped to <see cref="TorrentInfoResult"/>
/// after materialization.
/// </summary>
public class TorrentInfoQueryDto
{
    public string InfoHash { get; set; } = default!;
    public string? Resolution { get; set; }
    public int? Year { get; set; }
    public bool? Remastered { get; set; }
    public string? Codec { get; set; }
    public string[]? Audio { get; set; }
    public string? Quality { get; set; }
    public int[]? Episodes { get; set; }
    public int[]? Seasons { get; set; }
    public string[]? Languages { get; set; }
    public string? ParsedTitle { get; set; }
    public string? NormalizedTitle { get; set; }
    public string? RawTitle { get; set; }
    public string? Size { get; set; }
    public string Category { get; set; } = default!;
    public bool? Complete { get; set; }
    public int[]? Volumes { get; set; }
    public string[]? Hdr { get; set; }
    public string[]? Channels { get; set; }
    public bool? Dubbed { get; set; }
    public bool? Subbed { get; set; }
    public string? Edition { get; set; }
    public string? BitDepth { get; set; }
    public string? Bitrate { get; set; }
    public string? Network { get; set; }
    public bool? Extended { get; set; }
    public bool? Converted { get; set; }
    public bool? Hardcoded { get; set; }
    public string? Region { get; set; }
    public bool? Ppv { get; set; }
    public bool? Is3d { get; set; }
    public string? Site { get; set; }
    public bool? Proper { get; set; }
    public bool? Repack { get; set; }
    public bool? Retail { get; set; }
    public bool? Upscaled { get; set; }
    public bool? Unrated { get; set; }
    public bool? Documentary { get; set; }
    public string? EpisodeCode { get; set; }
    public string? Country { get; set; }
    public string? Container { get; set; }
    public string? Extension { get; set; }
    public bool? Torrent { get; set; }
    public float? Score { get; set; }
    public string? ImdbId { get; set; }
    public string? ImdbCategory { get; set; }
    public string? ImdbTitle { get; set; }
    public int? ImdbYear { get; set; }
    public bool? ImdbAdult { get; set; }
    public DateTime IngestedAt { get; set; }

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