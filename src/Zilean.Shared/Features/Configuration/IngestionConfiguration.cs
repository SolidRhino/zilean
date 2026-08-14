namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Configuration for generic ingestion of torrent metadata from Zurg, Zilean, and
/// generic HTTP endpoints. Bound from the <c>Zilean__Ingestion</c> env var section.
/// </summary>
public class IngestionConfiguration
{
    /// <summary>
    /// List of Zurg instance endpoints to scrape.
    /// Set via the <c>Zilean__Ingestion__ZurgInstances</c> env var section.
    /// </summary>
    public List<GenericEndpoint> ZurgInstances { get; set; } = [];

    /// <summary>
    /// List of remote Zilean instance endpoints to scrape.
    /// Set via the <c>Zilean__Ingestion__ZileanInstances</c> env var section.
    /// </summary>
    public List<GenericEndpoint> ZileanInstances { get; set; } = [];

    /// <summary>
    /// List of generic HTTP endpoints to scrape.
    /// Set via the <c>Zilean__Ingestion__GenericInstances</c> env var section.
    /// </summary>
    public List<GenericEndpoint> GenericInstances { get; set; } = [];

    /// <summary>
    /// Enables the ingestion scraping scheduler.
    /// Set via the <c>Zilean__Ingestion__EnableScraping</c> env var.
    /// </summary>
    public bool EnableScraping { get; set; } = false;

    /// <summary>
    /// Kubernetes service discovery configuration for auto-discovering ingestion endpoints.
    /// Set via the <c>Zilean__Ingestion__Kubernetes</c> env var section.
    /// </summary>
    public KubernetesConfiguration Kubernetes { get; set; } = new();

    /// <summary>
    /// Cron expression controlling the ingestion scrape schedule.
    /// Set via the <c>Zilean__Ingestion__ScrapeSchedule</c> env var.
    /// </summary>
    public string ScrapeSchedule { get; set; } = "0 * * * *";

    /// <summary>
    /// URL suffix appended to Zurg instance URLs when constructing the torrent list endpoint.
    /// Set via the <c>Zilean__Ingestion__ZurgEndpointSuffix</c> env var.
    /// </summary>
    public string ZurgEndpointSuffix { get; set; } = "/debug/torrents";

    /// <summary>
    /// URL suffix appended to remote Zilean instance URLs when constructing the torrent list endpoint.
    /// Set via the <c>Zilean__Ingestion__ZileanEndpointSuffix</c> env var.
    /// </summary>
    public string ZileanEndpointSuffix { get; set; } = "/torrents/all";

    /// <summary>
    /// HTTP request timeout in milliseconds for ingestion scrape requests.
    /// Set via the <c>Zilean__Ingestion__RequestTimeout</c> env var.
    /// </summary>
    public int RequestTimeout { get; set; } = 10000;
}