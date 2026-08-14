namespace Zilean.ApiService.Features.Dashboard.Components.Pages.Dashboard;

/// <summary>
/// View model representing torrent details displayed and edited in the Blazor dashboard grid.
/// </summary>
public class DashboardTorrentDetails
{
    /// <summary>
    /// The SHA-1 info hash identifying the torrent (40 hex characters).
    /// </summary>
    [Required]
    [StringLength(40)]
    public string InfoHash { get; set; } = default!;

    /// <summary>
    /// The DMM category of the torrent (e.g. movies, tv).
    /// </summary>
    public string Category { get; set; } = default!;

    /// <summary>
    /// The raw, unparsed release title as published.
    /// </summary>
    [Required]
    public string? RawTitle { get; set; }

    /// <summary>
    /// The Python-parsed title derived from <see cref="RawTitle"/>.
    /// </summary>
    public string? ParsedTitle { get; set; }

    /// <summary>
    /// Whether the torrent is flagged as trash (low quality/duplicate).
    /// </summary>
    public bool? Trash { get; set; } = false;

    /// <summary>
    /// The release year as a string (parsed to int when persisted).
    /// </summary>
    [Range(0, 9999, ErrorMessage = "Please enter valid Year between 0 and 9999")]
    public string? Year { get; set; }

    /// <summary>
    /// The file size in bytes as a string.
    /// </summary>
    [Required]
    [Range(0, long.MaxValue, ErrorMessage = "Please enter valid Filesize in Bytes")]
    public string? Size { get; set; }

    /// <summary>
    /// The IMDb identifier associated with the torrent, if any.
    /// </summary>
    public string? ImdbId { get; set; }

    /// <summary>
    /// Whether the torrent is adult content.
    /// </summary>
    public bool IsAdult { get; set; }

    /// <summary>
    /// Whether the category field was explicitly changed by the dashboard editor.
    /// </summary>
    public bool ChangeCategory { get; set; }

    /// <summary>
    /// Whether the trash flag was explicitly changed by the dashboard editor.
    /// </summary>
    public bool ChangeTrash { get; set; }

    /// <summary>
    /// Whether the year field was explicitly changed by the dashboard editor.
    /// </summary>
    public bool ChangeYear { get; set; }

    /// <summary>
    /// Whether the adult flag was explicitly changed by the dashboard editor.
    /// </summary>
    public bool ChangeAdult { get; set; }

    /// <summary>
    /// Whether the IMDb identifier was explicitly changed by the dashboard editor.
    /// </summary>
    public bool ChangeImdb { get; set; }

    /// <summary>
    /// Converts this view model to a <see cref="TorrentInfo"/> entity.
    /// </summary>
    /// <param name="dtd">The dashboard view model to convert.</param>
    /// <returns>A <see cref="TorrentInfo"/> populated from the view model.</returns>
    public static TorrentInfo ToTorrentInfo(DashboardTorrentDetails dtd) => new()
    {
        InfoHash = dtd.InfoHash,
        RawTitle = dtd.RawTitle,
        ParsedTitle = dtd.ParsedTitle,
        Trash = dtd.Trash,
        Year = !dtd.Year.IsNullOrWhiteSpace() ? int.Parse(dtd.Year) : null,
        Category = dtd.Category,
        Size = dtd.Size,
        ImdbId = dtd.ImdbId,
        IsAdult = dtd.IsAdult
    };

    /// <summary>
    /// Creates a dashboard view model from a <see cref="TorrentInfo"/> entity.
    /// </summary>
    /// <param name="ti">The torrent entity to convert.</param>
    /// <returns>A <see cref="DashboardTorrentDetails"/> populated from the entity.</returns>
    public static DashboardTorrentDetails FromTorrentInfo(TorrentInfo ti) => new()
    {
        InfoHash = ti.InfoHash,
        RawTitle = ti.RawTitle,
        ParsedTitle = ti.ParsedTitle,
        Trash = ti.Trash,
        Year = ti.Year.HasValue ? ti.Year.ToString() : null,
        Category = ti.Category,
        Size = ti.Size,
        ImdbId = ti.ImdbId,
        IsAdult = ti.IsAdult
    };
}