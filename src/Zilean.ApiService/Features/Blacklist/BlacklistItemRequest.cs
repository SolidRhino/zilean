// ReSharper disable InconsistentNaming
namespace Zilean.ApiService.Features.Blacklist;

/// <summary>
/// Request model for adding an item to the blacklist.
/// </summary>
public class BlacklistItemRequest
{
    /// <summary>
    /// The infohash of the torrent to blacklist.
    /// </summary>
    public required string info_hash { get; set; }

    /// <summary>
    /// The reason for blacklisting the item.
    /// </summary>
    public required string reason { get; set; }
}