namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Specifies a Kubernetes label selector and URL template for discovering ingestion endpoints.
/// </summary>
public class KubernetesSelector
{
    /// <summary>
    /// URL template with a <c>{0}</c> placeholder for the discovered service host/IP.
    /// </summary>
    public string UrlTemplate { get; set; } = "";

    /// <summary>
    /// Kubernetes label selector expression used to filter discovered services.
    /// </summary>
    public string LabelSelector { get; set; } = "";

    /// <summary>
    /// The type of ingestion endpoint to assign to discovered services.
    /// </summary>
    public GenericEndpointType EndpointType { get; set; } = GenericEndpointType.Zurg;
}