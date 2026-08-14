namespace Zilean.Shared.Features.Scraping;

/// <summary>
/// Describes a generic ingestion endpoint (e.g. Zurg or another Zilean instance) for streamed scraping.
/// </summary>
public class GenericEndpoint
{
    /// <summary>
    /// Gets or sets the base URL of the endpoint.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the type of generic endpoint determining how it is scraped.
    /// </summary>
    public GenericEndpointType? EndpointType { get; set; }

    /// <summary>
    /// Gets or sets the API key used to authenticate against the endpoint, if required.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the bearer token or authorization header value sent to the endpoint, if required.
    /// </summary>
    public string? Authorization { get; set; }

    /// <summary>
    /// Gets or sets an optional path suffix appended to the endpoint URL when fetching entries.
    /// </summary>
    public string? EndpointSuffix { get; set; }
}