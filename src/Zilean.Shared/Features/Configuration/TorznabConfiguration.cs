namespace Zilean.Shared.Features.Configuration;

/// <summary>
/// Configuration for the Torznab API endpoint.
/// Bound from the <c>Zilean__Torznab</c> env var section.
/// </summary>
public class TorznabConfiguration
{
    /// <summary>
    /// Enables the Torznab-compatible API endpoint used by *arr applications.
    /// Set via the <c>Zilean__Torznab__EnableEndpoint</c> env var.
    /// </summary>
    public bool EnableEndpoint { get; set; } = true;
}