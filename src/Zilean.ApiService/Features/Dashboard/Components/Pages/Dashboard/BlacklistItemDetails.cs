namespace Zilean.ApiService.Features.Dashboard.Components.Pages.Dashboard;

/// <summary>
/// View model representing a blacklisted item displayed in the dashboard grid.
/// </summary>
public class BlacklistItemDetails
{
    /// <summary>
    /// The BitTorrent info hash of the blacklisted item.
    /// </summary>
    [Required(ErrorMessage = "Info hash is required.")]
    public string? InfoHash { get; set; }

    /// <summary>
    /// The reason the item was blacklisted.
    /// </summary>
    [Required(ErrorMessage = "Reason is required.")]
    public string? Reason { get; set; }

    /// <summary>
    /// The UTC timestamp when the item was blacklisted.
    /// </summary>
    public DateTime? BlacklistedAt { get; set; }
}