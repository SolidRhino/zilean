namespace Zilean.Shared.Features.Torznab.Info;

/// <summary>
/// Provides static metadata for the Torznab RSS channel element (title, description, link, etc.).
/// </summary>
public class ChannelInfo
{
    /// <summary>
    /// The GitHub repository URL for the Zilean project.
    /// </summary>
    public const string GitHubRepo = "https://github.com/iPromKnight/zilean";
    /// <summary>
    /// The channel title displayed in Torznab RSS feed readers.
    /// </summary>
    public const string Title = "Zilean Indexer";
    /// <summary>
    /// The channel description displayed in Torznab RSS feed readers.
    /// </summary>
    public const string Description = "DMM Cached RD Indexer";
    /// <summary>
    /// The language code for the channel content.
    /// </summary>
    public const string Language = "en-US";
    /// <summary>
    /// The default search category for the channel.
    /// </summary>
    public const string Category = "search";
    /// <summary>
    /// The channel link URI pointing to the GitHub repository.
    /// </summary>
    public static Uri Link => new(GitHubRepo);
    /// <summary>
    /// Returns a default <see cref="ChannelInfo"/> instance representing the Zilean indexer.
    /// </summary>
    public static ChannelInfo ZileanIndexer => new();
}
