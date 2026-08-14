namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Specifies the authentication method for connecting to the Kubernetes API.
/// </summary>
public enum KubernetesAuthenticationType
{
    /// <summary>
    /// Authenticate using a local kubeconfig file.
    /// </summary>
    ConfigFile = 0,

    /// <summary>
    /// Authenticate using the in-cluster service account (RBAC).
    /// </summary>
    RoleBased = 1
}