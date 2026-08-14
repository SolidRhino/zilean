namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Configuration for Kubernetes-based service discovery of ingestion endpoints.
/// Bound from the <c>Zilean__Ingestion__Kubernetes</c> env var section.
/// </summary>
public class KubernetesConfiguration
{
    /// <summary>
    /// Enables Kubernetes service discovery for auto-discovering ingestion endpoints.
    /// Set via the <c>Zilean__Ingestion__Kubernetes__EnableServiceDiscovery</c> env var.
    /// </summary>
    public bool EnableServiceDiscovery { get; set; } = false;

    /// <summary>
    /// List of label selectors used to discover Kubernetes services for ingestion.
    /// Set via the <c>Zilean__Ingestion__Kubernetes__KubernetesSelectors</c> env var section.
    /// </summary>
    public List<KubernetesSelector> KubernetesSelectors { get; set; } = [];

    /// <summary>
    /// Path to the kubeconfig file used for cluster authentication.
    /// Set via the <c>Zilean__Ingestion__Kubernetes__KubeConfigFile</c> env var.
    /// </summary>
    public string KubeConfigFile { get; set; } = "/$HOME/.kube/config";

    /// <summary>
    /// Authentication method to use when connecting to the Kubernetes API.
    /// Set via the <c>Zilean__Ingestion__Kubernetes__AuthenticationType</c> env var.
    /// </summary>
    public KubernetesAuthenticationType AuthenticationType { get; set; } = KubernetesAuthenticationType.ConfigFile;
}