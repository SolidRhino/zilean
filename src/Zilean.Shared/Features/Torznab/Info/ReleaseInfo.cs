namespace Zilean.Shared.Features.Torznab.Info;

/// <summary>
/// Represents a single torrent release result returned in a Torznab RSS feed response.
/// </summary>
public class ReleaseInfo() : ICloneable
{
    /// <summary>
    /// The static seeder count reported for all Zilean releases (Debrid-based, so always high).
    /// </summary>
    public const long Seeders = 999;
    /// <summary>
    /// The static peer count reported for all Zilean releases.
    /// </summary>
    public const long Peers = 999;
    /// <summary>
    /// The origin label identifying results as coming from Zilean.
    /// </summary>
    public const string Origin = "Zilean";
    /// <summary>
    /// The release title as parsed from the torrent name.
    /// </summary>
    public string? Title { get; set; }
    /// <summary>
    /// A unique identifier for the release.
    /// </summary>
    public Guid? Guid { get; set; }
    /// <summary>
    /// The magnet URI for the torrent.
    /// </summary>
    public Uri? Magnet { get; set; }
    /// <summary>
    /// A URI to the release details page, if available.
    /// </summary>
    public Uri? Details { get; set; }
    /// <summary>
    /// The date the release was published.
    /// </summary>
    public DateTime PublishDate { get; set; }
    /// <summary>
    /// The Torznab category IDs associated with the release.
    /// </summary>
    public ICollection<int> Category { get; set; } = [];
    /// <summary>
    /// The size of the release in bytes, if known.
    /// </summary>
    public long? Size { get; set; }
    /// <summary>
    /// A human-readable description of the release, if available.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// The numeric IMDb ID (without <c>tt</c> prefix), if known.
    /// </summary>
    public long? Imdb { get; set; }
    /// <summary>
    /// The languages detected in the release name.
    /// </summary>
    public ICollection<string> Languages { get; set; } = [];
    /// <summary>
    /// The release year, if parsed from the name.
    /// </summary>
    public long? Year { get; set; }
    /// <summary>
    /// The torrent info hash (SHA1/magnet hash).
    /// </summary>
    public string? InfoHash { get; set; }
    /// <summary>
    /// Converts a byte count to gigabytes.
    /// </summary>
    /// <param name="size">The size in bytes, or <c>null</c>.</param>
    /// <returns>The size in gigabytes, or <c>null</c> if <paramref name="size"/> is <c>null</c>.</returns>
    public static double? GigabytesFromBytes(double? size) => size / 1024.0 / 1024.0 / 1024.0;

    private ReleaseInfo(ReleaseInfo copyFrom) : this()
    {
        Title = copyFrom.Title;
        Guid = copyFrom.Guid;
        Magnet = copyFrom.Magnet;
        Details = copyFrom.Details;
        PublishDate = copyFrom.PublishDate;
        Category = copyFrom.Category;
        Size = copyFrom.Size;
        Description = copyFrom.Description;
        Imdb = copyFrom.Imdb;
        Languages = copyFrom.Languages;
        Year = copyFrom.Year;
        InfoHash = copyFrom.InfoHash;
    }

    /// <summary>
    /// Creates a deep copy of this release info.
    /// </summary>
    /// <returns>A new <see cref="ReleaseInfo"/> with all fields copied.</returns>
    public virtual object Clone() => new ReleaseInfo(this);

    /// <summary>
    /// Returns a string representation of the release info for debugging.
    /// </summary>
    /// <returns>A formatted string listing all release fields.</returns>
    public override string ToString() =>
        $"[ReleaseInfo: Title={Title}, Guid={Guid}, Link={Magnet}, Details={Details}, PublishDate={PublishDate}, Category={Category}, Size={Size}, Description={Description}, Imdb={Imdb}, Seeders={Seeders}, Peers={Peers}, InfoHash={InfoHash}]";
}
